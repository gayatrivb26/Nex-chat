using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public class CreatePrivateConversationHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<CreatePrivateConversationCommand, ConversationDto>
{
    public async Task<ConversationDto> Handle(CreatePrivateConversationCommand cmd, CancellationToken ct)
    {
        var existing = await uow.Conversations.GetPrivateConversationAsync(cmd.UserId, cmd.OtherUserId, ct);
        if (existing != null) return await MapConversation(existing, cmd.UserId, uow, ct);

        var contact = await uow.Users.GetByIdAsync(cmd.OtherUserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var conversation = Conversation.CreatePrivate(cmd.UserId, cmd.OtherUserId);
        await uow.Conversations.AddAsync(conversation, ct);
        await uow.SaveChangesAsync(ct);

        await cache.RemoveAsync($"user:{cmd.UserId}:conversations", ct);
        await cache.RemoveAsync($"user:{cmd.OtherUserId}:conversations", ct);

        return await MapConversation(conversation, cmd.UserId, uow, ct);
    }

    public static async Task<ConversationDto> MapConversation(
        Conversation c, Guid currentUserId, IUnitOfWork uow, CancellationToken ct)
    {
        var unread = await uow.Conversations.GetUnreadCountAsync(c.Id, currentUserId, ct);
        var members = c.Members.Select(m => new ConversationMemberDto(
            m.UserId, m.User?.DisplayName, m.User?.AvatarUrl,
            m.Role.ToString(), m.IsMuted, m.JoinedAt,
            m.User?.Status.ToString() ?? "Offline")).ToList();
        return new ConversationDto(c.Id, c.Type.ToString(), c.Name, c.Description,
            c.AvatarUrl, null, unread, c.LastActivityAt, members);
    }
}
