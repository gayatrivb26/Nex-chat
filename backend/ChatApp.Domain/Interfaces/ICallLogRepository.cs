using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface ICallLogRepository : IRepository<CallLog>
{
    Task<IEnumerable<CallLog>> GetUserCallHistoryAsync(Guid userId, int skip = 0, int take = 30, CancellationToken ct = default);
    Task<IEnumerable<CallLog>> GetConversationCallsAsync(Guid conversationId, CancellationToken ct = default);
    Task<CallLog?> GetActiveCallAsync(Guid conversationId, CancellationToken ct = default);
}
