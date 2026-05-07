using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public class MarkMessagesReadHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<MarkMessagesReadCommand>
{
    public async Task Handle(MarkMessagesReadCommand cmd, CancellationToken ct)
    {
        var member = await uow.Conversations.GetMemberAsync(cmd.ConversationId, cmd.UserId, ct);
        if (member == null) return;

        member.LastReadMessageId = cmd.LastReadMessageId;
        member.LastReadAt = DateTime.UtcNow;

        await uow.Messages.BulkUpdateStatusAsync(
            new[] { cmd.LastReadMessageId }, cmd.UserId, MessageStatusType.Read, ct);

        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"user:{cmd.UserId}:unread_count", ct);
    }
}
