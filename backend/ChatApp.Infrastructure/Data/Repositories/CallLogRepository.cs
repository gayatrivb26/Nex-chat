using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class CallLogRepository(AppDbContext db) : Repository<CallLog>(db), ICallLogRepository
{
    public async Task<IEnumerable<CallLog>> GetUserCallHistoryAsync(
        Guid userId, int skip = 0, int take = 30, CancellationToken ct = default)
        => await Set
            .Include(c => c.Initiator)
            .Where(c => c.InitiatorId == userId ||
                (c.Conversation != null && c.Conversation.Members.Any(m => m.UserId == userId)))
            .OrderByDescending(c => c.StartedAt).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<IEnumerable<CallLog>> GetConversationCallsAsync(
        Guid conversationId, CancellationToken ct = default)
        => await Set.Where(c => c.ConversationId == conversationId)
            .OrderByDescending(c => c.StartedAt).ToListAsync(ct);

    public async Task<CallLog?> GetActiveCallAsync(Guid conversationId, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(c =>
            c.ConversationId == conversationId &&
            (c.Status == CallStatus.Initiated || c.Status == CallStatus.Ringing || c.Status == CallStatus.Answered), ct);
}
