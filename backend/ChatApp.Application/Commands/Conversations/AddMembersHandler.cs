using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public class AddMembersHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<AddMembersCommand>
{
    public async Task Handle(AddMembersCommand cmd, CancellationToken ct)
    {
        var conversation = await uow.Conversations.GetWithMembersAsync(cmd.ConversationId, ct)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var requestingMember = conversation.Members.FirstOrDefault(m => m.UserId == cmd.UserId);
        if (requestingMember == null || !requestingMember.CanManageMembers())
            throw new UnauthorizedAccessException("Insufficient permissions.");

        foreach (var newUserId in cmd.NewMemberIds)
        {
            if (conversation.Members.Any(m => m.UserId == newUserId && m.IsActive())) continue;
            conversation.Members.Add(ConversationMember.Create(conversation.Id, newUserId));
            var sysMsg = Message.CreateSystem(conversation.Id, $"User joined the group.");
            await uow.Messages.AddAsync(sysMsg, ct);
            await cache.RemoveAsync($"user:{newUserId}:conversations", ct);
        }

        uow.Conversations.Update(conversation);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"conversation:{cmd.ConversationId}:members", ct);
    }
}
