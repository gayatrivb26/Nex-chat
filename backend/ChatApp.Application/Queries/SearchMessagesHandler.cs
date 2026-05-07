using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public class SearchMessagesHandler(IUnitOfWork uow) : IRequestHandler<SearchMessagesQuery, PagedResult<MessageSearchResult>>
{
    public async Task<PagedResult<MessageSearchResult>> Handle(SearchMessagesQuery q, CancellationToken ct)
    {
        var isMember = await uow.Conversations.IsUserMemberAsync(q.ConversationId, q.UserId, ct);
        if (!isMember) throw new UnauthorizedAccessException("Not a member of this conversation.");

        var skip = (q.Page - 1) * q.PageSize;
        var messages = await uow.Messages.SearchMessagesAsync(q.ConversationId, q.Query, skip, q.PageSize, ct);

        var results = messages.Select(m => new MessageSearchResult(
            m.Id, m.ConversationId, m.Content,
            m.Sender == null ? null : new UserProfileDto(m.Sender.Id, m.Sender.Username,
                m.Sender.AvatarUrl, m.Sender.DisplayName, m.Sender.Bio,
                m.Sender.Status.ToString(), m.Sender.LastSeen, m.Sender.IsVerified),
            m.CreatedAt)).ToList();

        return PagedResult<MessageSearchResult>.Create(results, results.Count, q.Page, q.PageSize);
    }
}
