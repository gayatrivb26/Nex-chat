using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record CallAnsweredEvent(Guid CallId);
