using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MessagesReadEvent(Guid ConversationId, Guid UserId, Guid LastReadMessageId);
