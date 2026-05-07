using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Phone, string? Email);
