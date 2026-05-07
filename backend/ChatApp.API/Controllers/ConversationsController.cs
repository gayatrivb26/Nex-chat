using ChatApp.API.Extensions;
using ChatApp.Application.Commands.Conversations;
using ChatApp.Application.DTOs;
using ChatApp.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Authorize(Policy = "IsVerified")]
[Route("api/v1/conversations")]
public class ConversationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ConversationDto>>>> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetConversationsQuery(User.GetUserId(), page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<ConversationDto>>.Ok(result));
    }

    [HttpPost("private")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreatePrivate(CreatePrivateConversationRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreatePrivateConversationCommand(User.GetUserId(), request.OtherUserId), ct);
        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    [HttpPost("groups")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateGroup(CreateGroupRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateGroupCommand(
            User.GetUserId(), request.Name, request.Description, request.MemberIds, request.AvatarUrl), ct);
        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    [HttpPut("groups/{conversationId:guid}")]
    [Authorize(Policy = "IsGroupAdmin")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> UpdateGroup(Guid conversationId, UpdateGroupRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateGroupCommand(
            User.GetUserId(), conversationId, request.Name, request.Description, request.AvatarUrl), ct);
        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    [HttpPost("{conversationId:guid}/members")]
    [Authorize(Policy = "IsGroupAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> AddMembers(Guid conversationId, AddMembersRequest request, CancellationToken ct)
    {
        await mediator.Send(new AddMembersCommand(User.GetUserId(), conversationId, request.UserIds), ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("{conversationId:guid}/members/{userId:guid}")]
    [Authorize(Policy = "IsGroupAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveMember(Guid conversationId, Guid userId, CancellationToken ct)
    {
        await mediator.Send(new RemoveMemberCommand(User.GetUserId(), conversationId, userId), ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
