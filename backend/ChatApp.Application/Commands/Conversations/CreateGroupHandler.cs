using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public class CreateGroupHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<CreateGroupCommand, ConversationDto>
{
    public async Task<ConversationDto> Handle(CreateGroupCommand cmd, CancellationToken ct)
    {
        if (cmd.MemberIds.Count > 256)
            throw new InvalidOperationException("Groups cannot have more than 256 members.");

        var conversation = Conversation.CreateGroup(cmd.CreatorId, cmd.Name, cmd.Description, cmd.MemberIds);
        if (cmd.AvatarUrl != null) conversation.AvatarUrl = cmd.AvatarUrl;

        // Add system message
        var sysMsg = Message.CreateSystem(conversation.Id, $"Group '{cmd.Name}' created.");
        await uow.Messages.AddAsync(sysMsg, ct);

        await uow.Conversations.AddAsync(conversation, ct);
        await uow.SaveChangesAsync(ct);

        // Invalidate cache for all members
        foreach (var id in cmd.MemberIds.Append(cmd.CreatorId))
            await cache.RemoveAsync($"user:{id}:conversations", ct);

        return await CreatePrivateConversationHandler.MapConversation(conversation, cmd.CreatorId, uow, ct);
    }
}
