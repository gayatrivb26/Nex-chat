using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public class GetMessagesHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<GetMessagesQuery, IEnumerable<MessageDto>>
{
    public async Task<IEnumerable<MessageDto>> Handle(GetMessagesQuery q, CancellationToken ct)
    {
        var isMember = await uow.Conversations.IsUserMemberAsync(q.ConversationId, q.UserId, ct);
        if (!isMember) throw new UnauthorizedAccessException("Not a member of this conversation.");

        // Only use cache for first page (no before cursor)
        if (q.Before == null)
        {
            var cacheKey = $"conversation:{q.ConversationId}:messages";
            var cached = await cache.GetAsync<IEnumerable<MessageDto>>(cacheKey, ct);
            if (cached != null) return cached;
        }

        var messages = await uow.Messages.GetConversationMessagesAsync(q.ConversationId, q.Take, q.Before, ct);
        var dtos = messages.Select(MapMessage).ToList();

        if (q.Before == null)
            await cache.SetAsync($"conversation:{q.ConversationId}:messages", dtos, TimeSpan.FromHours(1), ct);

        return dtos;
    }

    private static MessageDto MapMessage(Domain.Entities.Message m)
    {
        var sender = m.Sender == null ? null : new UserProfileDto(
            m.Sender.Id, m.Sender.Username, m.Sender.AvatarUrl,
            m.Sender.DisplayName, m.Sender.Bio, m.Sender.Status.ToString(),
            m.Sender.LastSeen, m.Sender.IsVerified);

        var statuses = m.Statuses.Select(s =>
            new MessageStatusDto(s.UserId, s.Status.ToString(), s.DeliveredAt, s.ReadAt)).ToList();

        var reactions = m.Reactions.Select(r =>
            new ReactionDto(r.MessageId, r.UserId,
                r.User?.DisplayName, r.User?.AvatarUrl, r.Emoji, r.CreatedAt)).ToList();

        MessageDto? replyTo = null;
        if (m.ReplyToMessage != null)
            replyTo = MapMessage(m.ReplyToMessage);

        return new MessageDto(m.Id, m.ConversationId, m.SenderId, sender,
            m.Content, m.MessageType.ToString(), m.MediaUrl, m.ThumbnailUrl,
            m.FileName, m.FileSize, m.MediaDuration, m.MimeType,
            m.ReplyToMessageId, replyTo, m.IsEdited, m.IsDeleted,
            statuses, reactions, m.CreatedAt, m.EditedAt);
    }
}
