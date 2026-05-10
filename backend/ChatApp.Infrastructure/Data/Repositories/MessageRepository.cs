using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

        if (before.HasValue)
            query = query.Where(m => m.CreatedAt < before.Value);

        // Fetch newest N messages then re-order ascending for display
        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
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
            .Where(m => m.ConversationId == conversationId
                     && !m.IsDeleted
                     && m.Content != null
                     && EF.Functions.ToTsVector("english", m.Content)
                            .Matches(EF.Functions.PlainToTsQuery("english", query)))
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public async Task<IEnumerable<Message>> GetUndeliveredMessagesAsync(
        Guid userId, CancellationToken ct = default)
        => await Set
            .Include(m => m.Statuses)
            .Where(m => m.Statuses.Any(s => s.UserId == userId && s.Status == MessageStatusType.Sent))
            .ToListAsync(ct);

    /// <summary>
    /// Marks ALL messages in a conversation up to (and including) lastReadMessageId as read.
    /// This correctly handles marking multiple messages read when user scrolls up.
    /// </summary>
    public async Task BulkUpdateStatusAsync(
        IEnumerable<Guid> messageIds, Guid userId,
        MessageStatusType status, CancellationToken ct = default)
    {
        var ids = messageIds.ToList();
        await Db.MessageStatuses
            .Where(s => ids.Contains(s.MessageId)
                     && s.UserId == userId
                     && s.Status < status)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.ReadAt,
                    status == MessageStatusType.Read ? DateTime.UtcNow : (DateTime?)null)
                .SetProperty(x => x.DeliveredAt,
                    status >= MessageStatusType.Delivered ? DateTime.UtcNow : (DateTime?)null),
            ct);
    }

    /// <summary>
    /// Bulk-marks all messages in a conversation before a cutoff time as read for a user.
    /// Used when marking all messages up to lastReadMessageId as read.
    /// </summary>
    public async Task BulkMarkReadUpToAsync(
        Guid conversationId, Guid userId, DateTime upToCreatedAt, CancellationToken ct = default)
    {
        var unreadMessageIds = await Db.Messages
            .Where(m => m.ConversationId == conversationId
                     && m.SenderId != userId
                     && m.CreatedAt <= upToCreatedAt
                     && !m.IsDeleted)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (unreadMessageIds.Count == 0) return;

        // Upsert statuses
        var existingStatuses = await Db.MessageStatuses
            .Where(s => unreadMessageIds.Contains(s.MessageId) && s.UserId == userId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var status in existingStatuses.Where(s => s.Status < MessageStatusType.Read))
        {
            status.MarkRead();
        }

        // Add missing statuses (messages that have no status row yet for this user)
        var existingIds = existingStatuses.Select(s => s.MessageId).ToHashSet();
        var missingIds = unreadMessageIds.Where(id => !existingIds.Contains(id));
        foreach (var msgId in missingIds)
        {
            var newStatus = MessageStatus.Create(msgId, userId);
            newStatus.MarkRead();
            await Db.MessageStatuses.AddAsync(newStatus, ct);
        }

        await Db.SaveChangesAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid conversationId, Guid userId,
        Guid? lastReadMessageId, CancellationToken ct = default)
    {
        var query = Set.Where(m => m.ConversationId == conversationId
                                && m.SenderId != userId
                                && !m.IsDeleted);
        if (lastReadMessageId.HasValue)
        {
            var lastReadTime = await Set
                .Where(m => m.Id == lastReadMessageId.Value)
                .Select(m => m.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (lastReadTime != default)
                query = query.Where(m => m.CreatedAt > lastReadTime);
        }
        return await query.CountAsync(ct);
    }
}