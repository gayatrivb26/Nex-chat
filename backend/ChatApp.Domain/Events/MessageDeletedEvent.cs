using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MessageDeletedEvent(Guid MessageId, Guid ConversationId, bool ForEveryone);
