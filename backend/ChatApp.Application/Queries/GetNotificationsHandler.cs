using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public class GetNotificationsHandler(IUnitOfWork uow)
    : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery q, CancellationToken ct)
    {
        var skip = (q.Page - 1) * q.PageSize;
        var notifications = await uow.Notifications.GetUserNotificationsAsync(q.UserId, skip, q.PageSize, ct);

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id, n.Type, n.Title, n.Body, n.ImageUrl, n.IsRead, n.CreatedAt)).ToList();

        return PagedResult<NotificationDto>.Create(dtos, dtos.Count, q.Page, q.PageSize);
    }
}
