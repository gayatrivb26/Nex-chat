using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public class AddReactionHandler(IUnitOfWork uow) : IRequestHandler<AddReactionCommand, ReactionDto>
{
    public async Task<ReactionDto> Handle(AddReactionCommand cmd, CancellationToken ct)
    {
        var message = await uow.Messages.GetByIdAsync(cmd.MessageId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        var existing = message.Reactions.FirstOrDefault(r => r.UserId == cmd.UserId && r.Emoji == cmd.Emoji);
        if (existing != null) return new ReactionDto(cmd.MessageId, cmd.UserId, null, null, cmd.Emoji, existing.CreatedAt);

        var reaction = MessageReaction.Create(cmd.MessageId, cmd.UserId, cmd.Emoji);
        message.Reactions.Add(reaction);
        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);

        return new ReactionDto(cmd.MessageId, cmd.UserId, null, null, cmd.Emoji, reaction.CreatedAt);
    }
}
