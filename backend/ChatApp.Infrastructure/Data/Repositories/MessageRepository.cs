using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class MessageRepository(AppDbContext db) : Repository<Message>(db), IMessageRepository
{
    public async Task<IEnumerable<Message>> GetConversationMessagesAsync(
        Guid conversationId, int take = 50, DateTime? before = null, CancellationToken ct = default)
    {
        var query = Db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Statuses)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
            .Where(m => m.ConversationId == conversationId);

        if (before.HasValue) query = query.Where(m => m.CreatedAt < before.Value);

        return await query.OrderByDescending(m => m.CreatedAt).Take(take)
            .OrderBy(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task<Message?> GetWithStatusAsync(Guid messageId, CancellationToken ct = default)
        => await Set
            .Include(m => m.Statuses)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

    public async Task<IEnumerable<Message>> SearchMessagesAsync(
        Guid conversationId, string query, int skip = 0, int take = 20, CancellationToken ct = default)
        => await Set
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId &&
                       m.Content != null && EF.Functions.ToTsVector("english", m.Content)
                           .Matches(EF.Functions.PlainToTsQuery("english", query)))
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public async Task<IEnumerable<Message>> GetUndeliveredMessagesAsync(Guid userId, CancellationToken ct = default)
        => await Set
            .Include(m => m.Statuses)
            .Where(m => m.Statuses.Any(s => s.UserId == userId && s.Status == MessageStatusType.Sent))
            .ToListAsync(ct);

    public async Task BulkUpdateStatusAsync(IEnumerable<Guid> messageIds, Guid userId,
        MessageStatusType status, CancellationToken ct = default)
    {
        var ids = messageIds.ToList();
        await Db.MessageStatuses
            .Where(s => ids.Contains(s.MessageId) && s.UserId == userId && s.Status < status)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.ReadAt, status == MessageStatusType.Read ? DateTime.UtcNow : (DateTime?)null)
                .SetProperty(x => x.DeliveredAt, status >= MessageStatusType.Delivered ? DateTime.UtcNow : (DateTime?)null),
            ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId,
        Guid? lastReadMessageId, CancellationToken ct = default)
    {
        var query = Set.Where(m => m.ConversationId == conversationId && m.SenderId != userId);
        if (lastReadMessageId.HasValue)
        {
            var lastRead = await Set.FindAsync([lastReadMessageId.Value], ct);
            if (lastRead != null) query = query.Where(m => m.CreatedAt > lastRead.CreatedAt);
        }
        return await query.CountAsync(ct);
    }
}
