using System.IdentityModel.Tokens.Jwt;
using ChatApp.API.Extensions;
using ChatApp.Application.Commands.Auth;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController(IUnitOfWork uow, IPushNotificationService pushNotifications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> GetNotifications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var items = await uow.Notifications.GetUserNotificationsAsync(User.GetUserId(), skip, pageSize, ct);
        var dtos  = items.Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.ImageUrl, n.IsRead, n.CreatedAt));
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(dtos));
    }
 
    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid id, CancellationToken ct)
    {
        var n = await uow.Notifications.GetByIdAsync(id, ct);
        if (n == null || n.UserId != User.GetUserId()) return NotFound();
        n.MarkRead();
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
 
    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken ct)
    {
        await uow.Notifications.MarkAllReadAsync(User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("device-token")]
    public async Task<ActionResult<ApiResponse<object>>> RegisterDeviceToken(
        RegisterDeviceTokenRequest request,
        CancellationToken ct)
    {
        ValidateDeviceToken(request.FcmToken);
        if (string.IsNullOrWhiteSpace(request.DeviceType) || request.DeviceType.Length > 50)
            throw new InvalidOperationException("Invalid device type.");

        await pushNotifications.RegisterDeviceTokenAsync(User.GetUserId(), request.FcmToken, request.DeviceType, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("device-token/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> UnregisterDeviceToken(
        UnregisterDeviceTokenRequest request,
        CancellationToken ct)
    {
        ValidateDeviceToken(request.FcmToken);
        await pushNotifications.UnregisterDeviceTokenAsync(User.GetUserId(), request.FcmToken, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private static void ValidateDeviceToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096)
            throw new InvalidOperationException("Invalid device token.");
    }
}
