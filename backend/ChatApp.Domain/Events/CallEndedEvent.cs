using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record CallEndedEvent(Guid CallId, int DurationSeconds, string Reason);
