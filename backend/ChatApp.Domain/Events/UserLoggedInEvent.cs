using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record UserLoggedInEvent(Guid UserId, string IpAddress, string DeviceName);
