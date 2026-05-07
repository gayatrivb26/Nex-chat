using ChatApp.API.Extensions;
using ChatApp.API.Metrics;
using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace ChatApp.API.Hubs;

[Authorize(Policy = "IsVerified")]
public class ChatHub(
    IChatMessageService messageService,
    IPresenceService presence,
    IUnitOfWork uow,
    ILogger<ChatHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await presence.SetUserOnlineAsync(userId, Context.ConnectionId);
        ChatMetrics.ActiveSignalRConnections.Inc();
        await Clients.Others.SendAsync("UserPresenceChanged", userId, "online", DateTime.UtcNow);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User!.GetUserId();
        await presence.SetUserOfflineAsync(userId, Context.ConnectionId);
        ChatMetrics.ActiveSignalRConnections.Dec();
        if (!await presence.IsUserOnlineAsync(userId, Context.ConnectionAborted))
            await Clients.Others.SendAsync("UserPresenceChanged", userId, "offline", DateTime.UtcNow);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(SendMessageDto dto)
    {
        var userId = Context.User!.GetUserId();
        var sw = Stopwatch.StartNew();
        var message = await messageService.SendMessageAsync(userId, dto, Context.ConnectionAborted);
        sw.Stop();
        ChatMetrics.MessageDeliveryDuration.Observe(sw.Elapsed.TotalSeconds);
        ChatMetrics.MessagesSentTotal.Inc();
        await Clients.Group(ConversationGroup(dto.ConversationId)).SendAsync("ReceiveMessage", message);
    }

    public async Task EditMessage(EditMessageDto dto)
    {
        var userId = Context.User!.GetUserId();
        var message = await messageService.EditMessageAsync(userId, dto, Context.ConnectionAborted);
        await Clients.Group(ConversationGroup(message.ConversationId)).SendAsync("MessageEdited", message);
    }

    public async Task DeleteMessage(DeleteMessageDto dto)
    {
        var userId = Context.User!.GetUserId();
        var message = await uow.Messages.GetByIdAsync(dto.MessageId, Context.ConnectionAborted)
            ?? throw new HubException("Message not found.");
        await messageService.DeleteMessageAsync(userId, dto, Context.ConnectionAborted);
        await Clients.Group(ConversationGroup(message.ConversationId))
            .SendAsync("MessageDeleted", dto.MessageId, dto.ForEveryone);
    }

    public async Task StartTyping(Guid conversationId)
    {
        var userId = Context.User!.GetUserId();
        await EnsureMember(conversationId, userId);
        await presence.SetTypingAsync(conversationId, userId, true, Context.ConnectionAborted);
        await Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync("UserTyping", conversationId, userId, true);
    }

    public async Task StopTyping(Guid conversationId)
    {
        var userId = Context.User!.GetUserId();
        await presence.SetTypingAsync(conversationId, userId, false, Context.ConnectionAborted);
        await Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync("UserTyping", conversationId, userId, false);
    }

    public async Task MarkMessagesRead(Guid conversationId, Guid lastReadMessageId)
    {
        var userId = Context.User!.GetUserId();
        await messageService.MarkMessagesReadAsync(userId, conversationId, lastReadMessageId, Context.ConnectionAborted);
        await Clients.Group(ConversationGroup(conversationId))
            .SendAsync("MessageStatusUpdated", new MessageStatusDto(userId, "read", DateTime.UtcNow, DateTime.UtcNow));
    }

    public async Task Heartbeat()
    {
        var userId = Context.User!.GetUserId();
        await presence.UpdateHeartbeatAsync(userId, Context.ConnectionAborted);
    }

    public async Task ReactToMessage(ReactMessageDto dto)
    {
        var userId = Context.User!.GetUserId();
        var reaction = await messageService.ReactToMessageAsync(userId, dto, Context.ConnectionAborted);
        var message = await uow.Messages.GetByIdAsync(dto.MessageId, Context.ConnectionAborted);
        if (message != null)
            await Clients.Group(ConversationGroup(message.ConversationId)).SendAsync("ReactionAdded", reaction);
    }

    public async Task RemoveReaction(Guid messageId, string emoji)
    {
        var userId = Context.User!.GetUserId();
        var message = await uow.Messages.GetByIdAsync(messageId, Context.ConnectionAborted);
        await messageService.RemoveReactionAsync(userId, messageId, emoji, Context.ConnectionAborted);
        if (message != null)
            await Clients.Group(ConversationGroup(message.ConversationId)).SendAsync("ReactionRemoved", messageId, userId, emoji);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = Context.User!.GetUserId();
        await EnsureMember(conversationId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task LeaveConversation(Guid conversationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

    public async Task InitiateCall(InitiateCallDto dto)
    {
        var userId = Context.User!.GetUserId();
        await EnsureMember(dto.ConversationId, userId);
        if (!Enum.TryParse<CallType>(dto.CallType, true, out var callType))
            throw new HubException("Invalid call type.");

        var call = CallLog.Create(dto.ConversationId, userId, callType);
        await uow.CallLogs.AddAsync(call, Context.ConnectionAborted);
        await uow.SaveChangesAsync(Context.ConnectionAborted);
        ChatMetrics.ActiveCalls.Inc();

        await Clients.Group(UserGroup(dto.TargetUserId)).SendAsync("CallIncoming", MapCall(call));
    }

    public Task SendCallOffer(CallSignalDto dto)
        => Clients.Group(UserGroup(dto.TargetUserId)).SendAsync("CallIncoming", dto);

    public Task SendCallAnswer(CallSignalDto dto)
        => Clients.Group(UserGroup(dto.TargetUserId)).SendAsync("CallAnswered", dto.CallId);

    public Task SendIceCandidate(IceCandidateDto dto)
        => Clients.Group(UserGroup(dto.TargetUserId)).SendAsync("IceCandidateReceived", dto);

    public async Task EndCall(Guid callId)
    {
        var call = await uow.CallLogs.GetByIdAsync(callId, Context.ConnectionAborted)
            ?? throw new HubException("Call not found.");
        call.End();
        uow.CallLogs.Update(call);
        await uow.SaveChangesAsync(Context.ConnectionAborted);
        ChatMetrics.ActiveCalls.Dec();
        if (call.ConversationId.HasValue)
            await Clients.Group(ConversationGroup(call.ConversationId.Value)).SendAsync("CallEnded", callId, call.EndReason ?? "ended");
    }

    public async Task RejectCall(Guid callId)
    {
        var call = await uow.CallLogs.GetByIdAsync(callId, Context.ConnectionAborted)
            ?? throw new HubException("Call not found.");
        call.Reject();
        uow.CallLogs.Update(call);
        await uow.SaveChangesAsync(Context.ConnectionAborted);
        if (call.ConversationId.HasValue)
            await Clients.Group(ConversationGroup(call.ConversationId.Value)).SendAsync("CallRejected", callId);
    }

    private async Task EnsureMember(Guid conversationId, Guid userId)
    {
        if (!await uow.Conversations.IsUserMemberAsync(conversationId, userId, Context.ConnectionAborted))
        {
            logger.LogWarning("User {UserId} attempted to access conversation {ConversationId}", userId, conversationId);
            throw new HubException("Not a member of this conversation.");
        }
    }

    private static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";
    private static string UserGroup(Guid userId) => $"user:{userId}";

    private static CallDto MapCall(CallLog call)
        => new(call.Id, call.ConversationId ?? Guid.Empty, call.InitiatorId ?? Guid.Empty, null,
            call.CallType.ToString(), call.Status.ToString(), call.StartedAt,
            call.AnsweredAt, call.EndedAt, call.DurationSeconds);
}
