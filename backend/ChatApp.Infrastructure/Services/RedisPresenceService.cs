using ChatApp.Domain.Interfaces;
using StackExchange.Redis;

namespace ChatApp.Infrastructure.Services;

public class RedisPresenceService(IConnectionMultiplexer redis) : IPresenceService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SetUserOnlineAsync(Guid userId, string connectionId, CancellationToken ct = default)
    {
        await _db.SetAddAsync("online_users", userId.ToString());
        await _db.HashSetAsync($"user:{userId}:connections", connectionId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await _db.StringSetAsync($"user:{userId}:status", "online", TimeSpan.FromSeconds(30));
    }

    public async Task SetUserOfflineAsync(Guid userId, string connectionId, CancellationToken ct = default)
    {
        await _db.HashDeleteAsync($"user:{userId}:connections", connectionId);
        if ((await _db.HashLengthAsync($"user:{userId}:connections")) == 0)
        {
            await _db.SetRemoveAsync("online_users", userId.ToString());
            await _db.StringSetAsync($"user:{userId}:status", "offline", TimeSpan.FromSeconds(30));
            await _db.StringSetAsync($"user:{userId}:last_seen", DateTime.UtcNow.ToString("O"), TimeSpan.FromDays(7));
        }
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken ct = default)
        => await _db.SetContainsAsync("online_users", userId.ToString());

    public async Task<IEnumerable<Guid>> GetOnlineUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var tasks = userIds.Select(async id => (id, online: await IsUserOnlineAsync(id, ct)));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.online).Select(r => r.id);
    }

    public async Task UpdateHeartbeatAsync(Guid userId, CancellationToken ct = default)
        => await _db.StringSetAsync($"user:{userId}:status", "online", TimeSpan.FromSeconds(30));

    public async Task<DateTime?> GetLastSeenAsync(Guid userId, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync($"user:{userId}:last_seen");
        return value.HasValue && DateTime.TryParse(value!, out var parsed) ? parsed : null;
    }

    public async Task SetTypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken ct = default)
    {
        var key = $"conversation:{conversationId}:typing";
        if (isTyping)
        {
            await _db.SetAddAsync(key, userId.ToString());
            await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(3));
        }
        else
        {
            await _db.SetRemoveAsync(key, userId.ToString());
        }
    }

    public async Task<IEnumerable<Guid>> GetTypingUsersAsync(Guid conversationId, CancellationToken ct = default)
    {
        var values = await _db.SetMembersAsync($"conversation:{conversationId}:typing");
        return values.Select(v => Guid.TryParse(v!, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty);
    }
}
