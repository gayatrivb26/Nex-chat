using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record CallInitiatedEvent(Guid CallId, Guid ConversationId, Guid InitiatorId, CallType CallType);
