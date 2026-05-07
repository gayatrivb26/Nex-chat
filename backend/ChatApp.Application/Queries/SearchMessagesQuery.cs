using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public record SearchMessagesQuery(Guid UserId, Guid ConversationId, string Query, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<MessageSearchResult>>;
