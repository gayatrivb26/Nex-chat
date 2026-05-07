using System.Text.Json;
using ChatApp.Domain.Interfaces;
using StackExchange.Redis;

namespace ChatApp.Infrastructure.Services;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!, JsonOptions) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        => await _db.StringSetAsync(key, JsonSerializer.Serialize(value, JsonOptions), expiry);

    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        foreach (var endpoint in redis.GetEndPoints())
        {
            var server = redis.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(pattern: pattern))
                await _db.KeyDeleteAsync(key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => await _db.KeyExistsAsync(key);

    public async Task<long> IncrementAsync(string key, CancellationToken ct = default)
        => await _db.StringIncrementAsync(key);

    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
        => await _db.StringSetAsync(key, value, expiry, When.NotExists);

    public async Task BlacklistJtiAsync(string jti, TimeSpan expiry, CancellationToken ct = default)
        => await _db.StringSetAsync($"jwt:blacklist:{jti}", "1", expiry);

    public async Task<bool> IsJtiBlacklistedAsync(string jti, CancellationToken ct = default)
        => await _db.KeyExistsAsync($"jwt:blacklist:{jti}");
}
