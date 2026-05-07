using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public class RemoveMemberHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(RemoveMemberCommand cmd, CancellationToken ct)
    {
        var conversation = await uow.Conversations.GetWithMembersAsync(cmd.ConversationId, ct)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var requestingMember = conversation.Members.FirstOrDefault(m => m.UserId == cmd.RequestingUserId);
        var targetMember = conversation.Members.FirstOrDefault(m => m.UserId == cmd.TargetUserId);

        if (targetMember == null || !targetMember.IsActive())
            throw new KeyNotFoundException("Member not found.");

        // Can remove self (leave) OR admin can remove non-admin
        bool isSelf = cmd.RequestingUserId == cmd.TargetUserId;
        if (!isSelf && (requestingMember == null || !requestingMember.CanManageMembers()))
            throw new UnauthorizedAccessException("Insufficient permissions.");

        if (targetMember.IsOwner() && !isSelf)
            throw new InvalidOperationException("Cannot remove group owner.");

        targetMember.LeftAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);

        await cache.RemoveAsync($"conversation:{cmd.ConversationId}:members", ct);
        await cache.RemoveAsync($"user:{cmd.TargetUserId}:conversations", ct);
    }
}
