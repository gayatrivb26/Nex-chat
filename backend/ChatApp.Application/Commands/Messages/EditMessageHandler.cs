using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public class EditMessageHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<EditMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(EditMessageCommand cmd, CancellationToken ct)
    {
        var message = await uow.Messages.GetByIdAsync(cmd.MessageId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        if (message.SenderId != cmd.UserId)
            throw new UnauthorizedAccessException("Cannot edit another user's message.");

        if (message.MessageType != MessageType.Text)
            throw new InvalidOperationException("Only text messages can be edited.");

        if ((DateTime.UtcNow - message.CreatedAt).TotalHours > 24)
            throw new InvalidOperationException("Cannot edit messages older than 24 hours.");

        message.Edit(cmd.NewContent);
        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"conversation:{message.ConversationId}:messages", ct);

        return MapMessage(message);
    }

    private static MessageDto MapMessage(Message m) => new(
        m.Id, m.ConversationId, m.SenderId, null,
        m.Content, m.MessageType.ToString(), m.MediaUrl, m.ThumbnailUrl,
        m.FileName, m.FileSize, m.MediaDuration, m.MimeType,
        m.ReplyToMessageId, null, m.IsEdited, m.IsDeleted,
        new(), new(), m.CreatedAt, m.EditedAt);
}
