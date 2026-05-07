using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MemberRoleChangedEvent(Guid ConversationId, Guid UserId, MemberRole NewRole);
