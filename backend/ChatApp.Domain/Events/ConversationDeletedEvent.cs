using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record ConversationDeletedEvent(Guid ConversationId);
