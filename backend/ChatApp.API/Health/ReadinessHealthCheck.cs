using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ChatApp.API.Health;

public class ReadinessHealthCheck(
    AppDbContext db,
    IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx, CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();

        // PostgreSQL
        bool dbOk;
        try
        {
            dbOk = await db.Database.CanConnectAsync(ct);
            data["postgres"] = dbOk ? "healthy" : "unreachable";
        }
        catch (Exception ex)
        {
            dbOk = false;
            data["postgres"] = ex.Message;
        }

        // Redis
        bool redisOk;
        try
        {
            var ping = await redis.GetDatabase().PingAsync();
            redisOk = ping.TotalMilliseconds < 5000;
            data["redis_ms"] = (long)ping.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            redisOk = false;
            data["redis"] = ex.Message;
        }

        return dbOk && redisOk
            ? HealthCheckResult.Healthy("All dependencies healthy", data)
            : HealthCheckResult.Unhealthy("One or more dependencies are unavailable", data: data);
    }
}