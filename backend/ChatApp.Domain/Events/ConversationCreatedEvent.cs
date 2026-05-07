using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record ConversationCreatedEvent(Guid ConversationId, ConversationType Type, Guid CreatedById);
