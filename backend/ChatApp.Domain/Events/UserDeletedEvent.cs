using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record UserDeletedEvent(Guid UserId);
