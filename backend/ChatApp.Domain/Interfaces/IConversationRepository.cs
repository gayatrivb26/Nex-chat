using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetWithMembersAsync(Guid id, CancellationToken ct = default);
    Task<Conversation?> GetPrivateConversationAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(Guid userId, int skip = 0, int take = 30, CancellationToken ct = default);
    Task<bool> IsUserMemberAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
    Task<ConversationMember?> GetMemberAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
}
