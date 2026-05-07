using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public record GetNotificationsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<NotificationDto>>;
