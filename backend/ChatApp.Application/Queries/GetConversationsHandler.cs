using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public class GetConversationsHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<GetConversationsQuery, PagedResult<ConversationDto>>
{
    public async Task<PagedResult<ConversationDto>> Handle(GetConversationsQuery q, CancellationToken ct)
    {
        var cacheKey = $"user:{q.UserId}:conversations:p{q.Page}";
        var cached = await cache.GetAsync<PagedResult<ConversationDto>>(cacheKey, ct);
        if (cached != null) return cached;

        var skip = (q.Page - 1) * q.PageSize;
        var conversations = (await uow.Conversations.GetUserConversationsAsync(q.UserId, skip, q.PageSize, ct)).ToList();

        var dtos = new List<ConversationDto>();
        foreach (var c in conversations)
        {
            var unread = await uow.Conversations.GetUnreadCountAsync(c.Id, q.UserId, ct);
            var members = c.Members.Select(m => new ConversationMemberDto(
                m.UserId, m.User?.DisplayName, m.User?.AvatarUrl,
                m.Role.ToString(), m.IsMuted, m.JoinedAt,
                m.User?.Status.ToString() ?? "Offline")).ToList();

            MessageDto? lastMsg = null;
            if (c.LastMessage != null)
            {
                var lm = c.LastMessage;
                lastMsg = new MessageDto(lm.Id, lm.ConversationId, lm.SenderId, null,
                    lm.Content, lm.MessageType.ToString(), lm.MediaUrl, null,
                    lm.FileName, lm.FileSize, null, null, null, null,
                    lm.IsEdited, lm.IsDeleted, new(), new(), lm.CreatedAt, null);
            }

            dtos.Add(new ConversationDto(c.Id, c.Type.ToString(), c.Name, c.Description,
                c.AvatarUrl, lastMsg, unread, c.LastActivityAt, members));
        }

        var result = PagedResult<ConversationDto>.Create(dtos, dtos.Count, q.Page, q.PageSize);
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), ct);
        return result;
    }
}
