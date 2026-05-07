using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MessageEditedEvent(Guid MessageId, Guid ConversationId, string NewContent);
