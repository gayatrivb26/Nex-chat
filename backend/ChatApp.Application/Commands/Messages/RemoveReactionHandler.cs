using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public class RemoveReactionHandler(IUnitOfWork uow) : IRequestHandler<RemoveReactionCommand>
{
    public async Task Handle(RemoveReactionCommand cmd, CancellationToken ct)
    {
        var message = await uow.Messages.GetWithStatusAsync(cmd.MessageId, ct);
        if (message == null) return;

        var reaction = message.Reactions.FirstOrDefault(r => r.UserId == cmd.UserId && r.Emoji == cmd.Emoji);
        if (reaction != null)
        {
            message.Reactions.Remove(reaction);
            uow.Messages.Update(message);
            await uow.SaveChangesAsync(ct);
        }
    }
}
