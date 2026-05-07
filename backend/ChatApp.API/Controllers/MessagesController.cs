using ChatApp.API.Extensions;
using ChatApp.API.Metrics;
using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Authorize(Policy = "IsVerified")]
[Route("api/v1/messages")]
public class MessagesController(IMediator mediator, IChatMessageService chatMessageService) : ControllerBase
{
    [HttpGet("{conversationId:guid}")]
    [Authorize(Policy = "CanAccessConversation")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MessageDto>>>> GetMessages(
        Guid conversationId,
        [FromQuery] int take = 50,
        [FromQuery] DateTime? before = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMessagesQuery(User.GetUserId(), conversationId, take, before), ct);
        return Ok(ApiResponse<IEnumerable<MessageDto>>.Ok(result));
    }

    [HttpGet("{conversationId:guid}/search")]
    [Authorize(Policy = "CanAccessConversation")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageSearchResult>>>> Search(
        Guid conversationId,
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchMessagesQuery(User.GetUserId(), conversationId, query, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<MessageSearchResult>>.Ok(result));
    }

    [HttpPost]
    [EnableRateLimiting("messages")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Send(SendMessageDto request, CancellationToken ct)
    {
        var result = await chatMessageService.SendMessageAsync(User.GetUserId(), request, ct);
        ChatMetrics.MessagesSentTotal.Inc();
        return Ok(ApiResponse<MessageDto>.Ok(result));
    }

    [HttpPut("{messageId:guid}")]
    [Authorize(Policy = "IsMessageSender")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Edit(Guid messageId, EditMessageDto request, CancellationToken ct)
    {
        var result = await chatMessageService.EditMessageAsync(User.GetUserId(), request with { MessageId = messageId }, ct);
        return Ok(ApiResponse<MessageDto>.Ok(result));
    }

    [HttpDelete("{messageId:guid}")]
    [Authorize(Policy = "IsMessageSender")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid messageId, [FromQuery] bool forEveryone = false, CancellationToken ct = default)
    {
        await chatMessageService.DeleteMessageAsync(User.GetUserId(), new DeleteMessageDto(messageId, forEveryone), ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
