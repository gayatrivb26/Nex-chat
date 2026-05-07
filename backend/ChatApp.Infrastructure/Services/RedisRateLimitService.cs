using ChatApp.Domain.Interfaces;

namespace ChatApp.Infrastructure.Services;

public class RedisRateLimitService(ICacheService cache) : IRateLimitService
{
    public async Task<(bool allowed, int remaining, TimeSpan retryAfter)> CheckRateLimitAsync(
        string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        var count = await cache.IncrementAsync(key, ct);
        if (count == 1)
            await cache.SetAsync($"{key}:window", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), window, ct);

        var remaining = Math.Max(0, limit - (int)count);
        return (count <= limit, remaining, window);
    }
}
