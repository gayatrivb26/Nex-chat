using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IPushNotificationService
{
    Task SendToUserAsync(Guid userId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task RegisterDeviceTokenAsync(Guid userId, string fcmToken, string deviceType, CancellationToken ct = default);
    Task UnregisterDeviceTokenAsync(Guid userId, string fcmToken, CancellationToken ct = default);
}
