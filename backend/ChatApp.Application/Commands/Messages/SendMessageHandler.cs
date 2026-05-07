using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public class SendMessageHandler(
    IUnitOfWork uow,
    ICacheService cache,
    IPresenceService presence,
    ILogger<SendMessageHandler> logger) : IRequestHandler<SendMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var isMember = await uow.Conversations.IsUserMemberAsync(cmd.ConversationId, cmd.SenderId, ct);
        if (!isMember) throw new UnauthorizedAccessException("Not a member of this conversation.");

        var message = Message.Create(cmd.ConversationId, cmd.SenderId, cmd.MessageType,
            cmd.Content, cmd.EncryptedContent, cmd.ReplyToMessageId);

        if (cmd.MediaUrl != null)
            message.SetMedia(cmd.MediaUrl, null, cmd.FileName, cmd.FileSize, cmd.MimeType);

        await uow.Messages.AddAsync(message, ct);
        await uow.SaveChangesAsync(ct);

        // Invalidate conversation message cache
        await cache.RemoveAsync($"conversation:{cmd.ConversationId}:messages", ct);

        logger.LogDebug("Message {MessageId} saved to DB for conversation {ConvId}",
            message.Id, cmd.ConversationId);

        return MapMessage(message);
    }

    private static MessageDto MapMessage(Message m) => new(
        m.Id, m.ConversationId, m.SenderId, null,
        m.Content, m.MessageType.ToString(), m.MediaUrl, m.ThumbnailUrl,
        m.FileName, m.FileSize, m.MediaDuration, m.MimeType,
        m.ReplyToMessageId, null, m.IsEdited, m.IsDeleted,
        new(), new(), m.CreatedAt, m.EditedAt);
}
