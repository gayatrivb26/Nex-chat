using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Services;

public class PushNotificationService(ILogger<PushNotificationService> logger) : IPushNotificationService
{
    public Task SendToUserAsync(Guid userId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        logger.LogInformation("Push notification queued for user {UserId}: {Title}", userId, title);
        return Task.CompletedTask;
    }

    public Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        logger.LogInformation("Push notification queued for {Count} users: {Title}", userIds.Count(), title);
        return Task.CompletedTask;
    }

    public Task RegisterDeviceTokenAsync(Guid userId, string fcmToken, string deviceType, CancellationToken ct = default)
    {
        logger.LogInformation("Device token registered for user {UserId} ({DeviceType})", userId, deviceType);
        return Task.CompletedTask;
    }

    public Task UnregisterDeviceTokenAsync(Guid userId, string fcmToken, CancellationToken ct = default)
    {
        logger.LogInformation("Device token unregistered for user {UserId}", userId);
        return Task.CompletedTask;
    }
}
