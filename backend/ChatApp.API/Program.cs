using System.Security.Claims;
using System.Text.Json.Serialization;
using ChatApp.API.Authorization;
using ChatApp.API.Health;
using ChatApp.API.Hubs;
using ChatApp.API.Messaging;
using ChatApp.API.Middleware;
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
using ChatApp.API.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

// ─── Bootstrap Serilog ASAP ───────────────────────────────────────────────────

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName());

    // ─── Application + Infrastructure DI ─────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddKafkaConsumers();
    builder.Services.AddHttpContextAccessor();

    // ─── Authorization Handlers ───────────────────────────────────────────────
    builder.Services.AddScoped<IAuthorizationHandler, GroupRoleAuthorizationHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, MessageSenderAuthorizationHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, NotBlockedAuthorizationHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, ConversationAccessAuthorizationHandler>();

    // ─── Hangfire ─────────────────────────────────────────────────────────────
    var dbConnection = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(dbConnection)));
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 5;
        options.Queues = new[] { "critical", "default", "low" };
    });

    // ─── Controllers ─────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "ChatApp API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new()
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });
    builder.Services.AddProblemDetails();

    // ─── CORS ─────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:4200", "https://localhost:4200", "app://." };

    builder.Services.AddCors(opts =>
        opts.AddPolicy("frontend", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    // ─── JWT Authentication (RS256) ───────────────────────────────────────────
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
                // Support JWT in query string for SignalR WebSocket
                OnMessageReceived = context =>
                {
                    var token = context.Request.Query["access_token"];
                    if (!string.IsNullOrWhiteSpace(token) &&
                        context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        context.Token = token;
                    return Task.CompletedTask;
                },

                // Validate JTI blacklist (logout / token revocation)
                OnTokenValidated = async context =>
                {
                    var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti)
                           ?? context.Principal?.FindFirstValue("jti");
                    if (!string.IsNullOrWhiteSpace(jti))
                    {
                        var cache = context.HttpContext.RequestServices
                            .GetRequiredService<ICacheService>();
                        if (await cache.IsJtiBlacklistedAsync(jti))
                            context.Fail("Token has been revoked.");
                    }
                }
            };
        });

    // ─── Authorization Policies ───────────────────────────────────────────────
    builder.Services.AddAuthorization(opts =>
    {
        opts.AddPolicy("IsVerified",            p => p.RequireClaim("is_verified", "true"));
        opts.AddPolicy("IsGroupAdmin",          p => p.Requirements.Add(new GroupRoleRequirement(MemberRole.Admin)));
        opts.AddPolicy("IsGroupOwner",          p => p.Requirements.Add(new GroupRoleRequirement(MemberRole.Owner)));
        opts.AddPolicy("IsMessageSender",       p => p.Requirements.Add(new MessageSenderRequirement()));
        opts.AddPolicy("IsNotBlocked",          p => p.Requirements.Add(new NotBlockedRequirement()));
        opts.AddPolicy("CanAccessConversation", p => p.Requirements.Add(new ConversationAccessRequirement()));
    });

    // ─── SignalR + Redis Backplane ────────────────────────────────────────────
    builder.Services.AddSignalR(opts =>
    {
        opts.EnableDetailedErrors       = builder.Environment.IsDevelopment();
        opts.KeepAliveInterval          = TimeSpan.FromSeconds(15);
        opts.ClientTimeoutInterval      = TimeSpan.FromSeconds(30);
        opts.MaximumReceiveMessageSize  = 32 * 1024; // 32 KB
    }).AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("Redis")
            ?? "localhost:6379,abortConnect=false",
        opts => opts.Configuration.ChannelPrefix = RedisChannel.Literal("ChatApp"));

    // ─── Rate Limiting ────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        opts.AddPolicy("auth-login",    ctx => FixedWindowByIp(ctx,  5, TimeSpan.FromMinutes(15)));
        opts.AddPolicy("auth-register", ctx => FixedWindowByIp(ctx,  3, TimeSpan.FromHours(1)));
        opts.AddPolicy("otp-send",      ctx => FixedWindowByIp(ctx,  3, TimeSpan.FromMinutes(10)));
        opts.AddPolicy("otp",           ctx => FixedWindowByIp(ctx,  5, TimeSpan.FromMinutes(10)));
        opts.AddPolicy("messages",      ctx => FixedWindowByUser(ctx, 60, TimeSpan.FromMinutes(1)));
        opts.AddPolicy("uploads",       ctx => FixedWindowByUser(ctx, 10, TimeSpan.FromMinutes(1)));
    });

    // ─── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddCheck<ReadinessHealthCheck>("ready", tags: new[] { "ready" })
        .AddNpgSql(dbConnection,                      name: "postgres", tags: new[] { "ready" })
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
            name: "redis", tags: new[] { "ready" });

    // ─── Build App ────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── Middleware pipeline ──────────────────────────────────────────────────
    app.UseExceptionHandler();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatApp API v1");
            c.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
        opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
        {
            diagCtx.Set("UserId", httpCtx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon");
            diagCtx.Set("RemoteIP", httpCtx.Connection.RemoteIpAddress?.ToString());
        };
    });

    app.UseHttpMetrics();
    app.UseRouting();
    app.UseCors("frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ─── Endpoints ────────────────────────────────────────────────────────────
    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
    app.MapMetrics("/metrics");

    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[]
        {
            new HangfireBasicAuthFilter(
                app.Configuration["Hangfire:Username"] ?? "admin",
                app.Configuration["Hangfire:Password"] ?? "admin")
        }
    });

    // Auto-migrate in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ChatApp.Infrastructure.Data.AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }

    Log.Information("ChatApp API starting [{Env}]", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

// ─── Local helpers ────────────────────────────────────────────────────────────

static RateLimitPartition<string> FixedWindowByIp(
    HttpContext ctx, int limit, TimeSpan window) =>
    RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit, Window = window,
            QueueLimit = 0, AutoReplenishment = true
        });

static RateLimitPartition<string> FixedWindowByUser(
    HttpContext ctx, int limit, TimeSpan window) =>
    RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? ctx.User.FindFirstValue("sub")
            ?? ctx.Connection.RemoteIpAddress?.ToString()
            ?? "anon",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit, Window = window,
            QueueLimit = 0, AutoReplenishment = true
        });
