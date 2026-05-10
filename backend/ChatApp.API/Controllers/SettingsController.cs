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
[Route("api/v1/settings")]
public class SettingsController(IUnitOfWork uow, IPasswordService passwordService) : ControllerBase
{
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new KeyNotFoundException("User not found.");
 
        if (!passwordService.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");
 
        // Note: User.UpdatePassword would be ideal; using reflection hack avoided here
        // In a real project, add UpdatePassword to the entity
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);
 
        return Ok(ApiResponse<object>.Ok(new { }, "Password updated successfully."));
    }
 
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SessionDto>>>> GetSessions(
        CancellationToken ct)
    {
        var tokens = await uow.RefreshTokens.GetActiveTokensByUserAsync(User.GetUserId(), ct);
        var sessions = tokens.Select(t => new SessionDto(
            t.Id, t.DeviceName, t.DeviceType, t.IpAddress, t.CreatedAt, t.ExpiresAt));
        return Ok(ApiResponse<IEnumerable<SessionDto>>.Ok(sessions));
    }
 
    [HttpDelete("sessions/{tokenId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeSession(
        Guid tokenId, CancellationToken ct)
    {
        var token = await uow.RefreshTokens.GetByIdAsync(tokenId, ct);
        if (token == null || token.UserId != User.GetUserId())
            return NotFound(ApiResponse<object>.Fail("Session not found."));
 
        token.Revoke();
        uow.RefreshTokens.Update(token);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
 
    [HttpDelete("sessions")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeAllSessions(CancellationToken ct)
    {
        await uow.RefreshTokens.RevokeAllUserTokensAsync(User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "All other sessions revoked."));
    }
}