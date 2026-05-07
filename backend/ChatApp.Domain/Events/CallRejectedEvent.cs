using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record CallRejectedEvent(Guid CallId, Guid RejectedById);
