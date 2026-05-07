using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IPresenceService
{
    Task SetUserOnlineAsync(Guid userId, string connectionId, CancellationToken ct = default);
    Task SetUserOfflineAsync(Guid userId, string connectionId, CancellationToken ct = default);
    Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Guid>> GetOnlineUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task UpdateHeartbeatAsync(Guid userId, CancellationToken ct = default);
    Task<DateTime?> GetLastSeenAsync(Guid userId, CancellationToken ct = default);
    Task SetTypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken ct = default);
    Task<IEnumerable<Guid>> GetTypingUsersAsync(Guid conversationId, CancellationToken ct = default);
}
