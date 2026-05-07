using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MemberAddedEvent(Guid ConversationId, Guid UserId, Guid AddedById);
