using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MemberRemovedEvent(Guid ConversationId, Guid UserId, Guid RemovedById);
