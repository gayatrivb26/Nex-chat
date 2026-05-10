using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetConversationMessagesAsync(
        Guid conversationId, int take = 50, DateTime? before = null, CancellationToken ct = default);
    Task<Message?> GetWithStatusAsync(Guid messageId, CancellationToken ct = default);
    Task<IEnumerable<Message>> SearchMessagesAsync(
        Guid conversationId, string query, int skip = 0, int take = 20, CancellationToken ct = default);
    Task<IEnumerable<Message>> GetUndeliveredMessagesAsync(Guid userId, CancellationToken ct = default);
    Task BulkUpdateStatusAsync(IEnumerable<Guid> messageIds, Guid userId,
        MessageStatusType status, CancellationToken ct = default);
    Task BulkMarkReadUpToAsync(Guid conversationId, Guid userId, DateTime upToCreatedAt, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, Guid? lastReadMessageId, CancellationToken ct = default);
}
