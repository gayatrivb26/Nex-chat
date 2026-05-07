using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public class DeleteMessageHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<DeleteMessageCommand>
{
    public async Task Handle(DeleteMessageCommand cmd, CancellationToken ct)
    {
        var message = await uow.Messages.GetByIdAsync(cmd.MessageId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        if (cmd.ForEveryone && message.SenderId != cmd.UserId)
            throw new UnauthorizedAccessException("Only the sender can delete for everyone.");

        if (cmd.ForEveryone)
            message.DeleteForAll();
        else
            message.DeleteForSender();

        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"conversation:{message.ConversationId}:messages", ct);
    }
}
