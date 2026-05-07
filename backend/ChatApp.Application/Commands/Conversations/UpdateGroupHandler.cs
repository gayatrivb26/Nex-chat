using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public class UpdateGroupHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<UpdateGroupCommand, ConversationDto>
{
    public async Task<ConversationDto> Handle(UpdateGroupCommand cmd, CancellationToken ct)
    {
        var conversation = await uow.Conversations.GetWithMembersAsync(cmd.ConversationId, ct)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var member = conversation.Members.FirstOrDefault(m => m.UserId == cmd.UserId)
            ?? throw new UnauthorizedAccessException("Not a member.");

        if (!member.CanManageMembers()) throw new UnauthorizedAccessException("Insufficient permissions.");

        if (cmd.Name != null) conversation.Name = cmd.Name;
        if (cmd.Description != null) conversation.Description = cmd.Description;
        if (cmd.AvatarUrl != null) conversation.AvatarUrl = cmd.AvatarUrl;

        uow.Conversations.Update(conversation);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"conversation:{cmd.ConversationId}:members", ct);

        return await CreatePrivateConversationHandler.MapConversation(conversation, cmd.UserId, uow, ct);
    }
}
