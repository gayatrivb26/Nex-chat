using System.Security.Claims;
using System.Text.Json.Serialization;
using ChatApp.API.Health;
using ChatApp.API.Hubs;
using ChatApp.API.Middleware;
using ChatApp.API.Authorization;
using ChatApp.Application;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure;
using ChatApp.Infrastructure.Security;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Prometheus;
using Serilog;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) =>
{
    logger.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, GroupRoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, MessageSenderAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, NotBlockedAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ConversationAccessAuthorizationHandler>();

var dbConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? "Host=localhost;Port=5432;Database=chatapp;Username=postgres;Password=postgres";

builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(dbConnection)));
builder.Services.AddHangfireServer();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200", "https://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtKeyProvider, IConfiguration>((options, keyProvider, configuration) =>
    {
        var issuer = configuration["Jwt:Issuer"] ?? "ChatApp";
        var audience = configuration["Jwt:Audience"] ?? "ChatApp.Client";
        options.TokenValidationParameters = keyProvider.CreateValidationParameters(issuer, audience);
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti)
                    ?? context.Principal?.FindFirstValue("jti");
                if (!string.IsNullOrWhiteSpace(jti))
                {
                    var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
                    if (await cache.IsJtiBlacklistedAsync(jti))
                        context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsVerified", policy => policy.RequireClaim("is_verified", "true"));
    options.AddPolicy("IsGroupAdmin", policy => policy.Requirements.Add(new GroupRoleRequirement(MemberRole.Admin)));
    options.AddPolicy("IsGroupOwner", policy => policy.Requirements.Add(new GroupRoleRequirement(MemberRole.Owner)));
    options.AddPolicy("IsMessageSender", policy => policy.Requirements.Add(new MessageSenderRequirement()));
    options.AddPolicy("IsNotBlocked", policy => policy.Requirements.Add(new NotBlockedRequirement()));
    options.AddPolicy("CanAccessConversation", policy => policy.Requirements.Add(new ConversationAccessRequirement()));
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 32 * 1024;
}).AddStackExchangeRedis(
    builder.Configuration.GetConnectionString("Redis")
        ?? builder.Configuration["ConnectionStrings:Redis"]
        ?? "localhost:6379,abortConnect=false",
    options => options.Configuration.ChannelPrefix = RedisChannel.Literal("ChatApp"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-login", context => FixedWindowByIp(context, 5, TimeSpan.FromMinutes(15)));
    options.AddPolicy("auth-register", context => FixedWindowByIp(context, 3, TimeSpan.FromHours(1)));
    options.AddPolicy("otp-send", context => FixedWindowByIp(context, 3, TimeSpan.FromMinutes(10)));
    options.AddPolicy("otp", context => FixedWindowByIp(context, 5, TimeSpan.FromMinutes(10)));
    options.AddPolicy("messages", context => FixedWindowByUser(context, 60, TimeSpan.FromMinutes(1)));
    options.AddPolicy("uploads", context => FixedWindowByUser(context, 10, TimeSpan.FromMinutes(1)));
});

builder.Services.AddHealthChecks()
    .AddCheck<ReadinessHealthCheck>("ready");

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseHttpMetrics();
app.UseRouting();
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapMetrics("/metrics");
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHangfireDashboard("/hangfire");

app.Run();

static RateLimitPartition<string> FixedWindowByIp(HttpContext context, int permitLimit, TimeSpan window)
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0,
        AutoReplenishment = true
    });
}

static RateLimitPartition<string> FixedWindowByUser(HttpContext context, int permitLimit, TimeSpan window)
{
    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.User.FindFirstValue("sub")
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";

    return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0,
        AutoReplenishment = true
    });
}
