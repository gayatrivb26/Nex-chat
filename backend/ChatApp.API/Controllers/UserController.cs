using System.IdentityModel.Tokens.Jwt;
using ChatApp.API.Extensions;
using ChatApp.Application.Commands.Auth;
using ChatApp.Application.DTOs;
using ChatApp.Application.Queries;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public class UsersController(IMediator mediator, IUnitOfWork uow) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMyProfile(CancellationToken ct)
    {
        var profile = await mediator.Send(
            new GetUserProfileQuery(User.GetUserId(), User.GetUserId()), ct);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }
 
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile(
        Guid userId, CancellationToken ct)
    {
        var profile = await mediator.Send(
            new GetUserProfileQuery(User.GetUserId(), userId), ct);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }
 
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserProfileDto>>>> Search(
        [FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(ApiResponse<object>.Fail("Query must be at least 2 characters."));
 
        var results = await mediator.Send(
            new SearchUsersQuery(User.GetUserId(), q, Math.Min(limit, 50)), ct);
        return Ok(ApiResponse<IEnumerable<UserProfileDto>>.Ok(results));
    }
 
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");
 
        user.UpdateProfile(request.DisplayName, request.Bio, request.AvatarUrl);
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);
 
        return Ok(ApiResponse<UserProfileDto>.Ok(new UserProfileDto(
            user.Id, user.Username, user.AvatarUrl, user.DisplayName,
            user.Bio, user.Status.ToString(), user.LastSeen, user.IsVerified)));
    }
}