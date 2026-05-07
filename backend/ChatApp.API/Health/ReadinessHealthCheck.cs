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
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        var dbOk = await db.Database.CanConnectAsync(cancellationToken);
        data["postgres"] = dbOk ? "ok" : "unavailable";

        var redisPing = await redis.GetDatabase().PingAsync();
        data["redis_ms"] = redisPing.TotalMilliseconds;

        return dbOk && redis.IsConnected
            ? HealthCheckResult.Healthy("Ready", data)
            : HealthCheckResult.Unhealthy("Dependencies are not ready", data: data);
    }
}
