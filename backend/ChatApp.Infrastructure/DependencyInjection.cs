using ChatApp.Application.Interfaces;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Data.Repositories;
using ChatApp.Infrastructure.Jobs;
using ChatApp.Infrastructure.Messaging;
using ChatApp.Infrastructure.Security;
using ChatApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
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

        services.AddSingleton<IMinioClient>(_ =>
        {
            var endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
            var accessKey = configuration["MinIO:AccessKey"]
                ?? throw new InvalidOperationException("MinIO access key is not configured.");
            var secretKey = configuration["MinIO:SecretKey"]
                ?? throw new InvalidOperationException("MinIO secret key is not configured.");
            var useSsl = bool.TryParse(configuration["MinIO:UseSSL"], out var configuredUseSsl) && configuredUseSsl;

            return new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSsl)
                .Build();
        });

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IVirusScanService, VirusScanService>();
        services.AddScoped<IMediaProcessingService, MediaProcessingService>();
        services.AddScoped<MediaProcessingJob>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IRateLimitService, RedisRateLimitService>();

        return services;
    }
}
