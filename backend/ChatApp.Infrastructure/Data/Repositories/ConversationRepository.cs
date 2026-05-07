using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class ConversationRepository(AppDbContext db) : Repository<Conversation>(db), IConversationRepository
{
    public async Task<Conversation?> GetWithMembersAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(c => c.Members).ThenInclude(m => m.User)
            .Include(c => c.LastMessage).ThenInclude(m => m!.Sender)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Conversation?> GetPrivateConversationAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
        => await Set
            .Include(c => c.Members)
            .Where(c => c.Type == ConversationType.Private)
            .Where(c => c.Members.Any(m => m.UserId == userId1) && c.Members.Any(m => m.UserId == userId2))
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(
        Guid userId, int skip = 0, int take = 30, CancellationToken ct = default)
        => await Set
            .Include(c => c.Members).ThenInclude(m => m.User)
            .Include(c => c.LastMessage).ThenInclude(m => m!.Sender)
            .Where(c => c.Members.Any(m => m.UserId == userId))
            .OrderByDescending(c => c.LastActivityAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public async Task<bool> IsUserMemberAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        => await Db.ConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null, ct);

    public async Task<ConversationMember?> GetMemberAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        => await Db.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId, ct);

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        var member = await Db.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId, ct);
        if (member == null) return 0;

        var query = Db.Messages.Where(m => m.ConversationId == conversationId && m.SenderId != userId);
        if (member.LastReadMessageId.HasValue)
        {
            var lastRead = await Db.Messages.FindAsync([member.LastReadMessageId.Value], ct);
            if (lastRead != null) query = query.Where(m => m.CreatedAt > lastRead.CreatedAt);
        }
        return await query.CountAsync(ct);
    }
}
