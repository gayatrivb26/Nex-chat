using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public record GetConversationsQuery(Guid UserId, int Page = 1, int PageSize = 30) : IRequest<PagedResult<ConversationDto>>;
