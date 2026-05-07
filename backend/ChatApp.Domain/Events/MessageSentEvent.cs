using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MessageSentEvent(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    MessageType MessageType,
    DateTime SentAt);
