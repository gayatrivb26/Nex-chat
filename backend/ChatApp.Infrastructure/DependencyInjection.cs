using ChatApp.Application.Interfaces;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Data.Repositories;
using ChatApp.Infrastructure.Messaging;
using ChatApp.Infrastructure.Security;
using ChatApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ChatApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnection = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? "Host=localhost;Port=5432;Database=chatapp;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(dbConnection, npgsql => npgsql.EnableRetryOnFailure(5)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["ConnectionStrings:Redis"]
            ?? "localhost:6379,abortConnect=false";

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IPresenceService, RedisPresenceService>();
        services.AddScoped<IOtpService, RedisOtpService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<JwtKeyProvider>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IVirusScanService, VirusScanService>();
        services.AddScoped<IMediaProcessingService, MediaProcessingService>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IRateLimitService, RedisRateLimitService>();

        return services;
    }
}
