using System.Text.Json;
using ChatApp.Infrastructure.Services;
using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Worker.Consumers;

public class PushNotificationConsumer
{
    private readonly IPushNotificationService _push;
    private readonly IPresenceService _presence;
    private readonly ILogger<PushNotificationConsumer> _logger;

    public PushNotificationConsumer(IPushNotificationService push, IPresenceService presence, ILogger<PushNotificationConsumer> logger)
    {
        _push = push;
        _presence = presence;
        _logger = logger;
    }

    public async Task HandleAsync(string key, string payload, CancellationToken ct = default)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageSentEvent>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (messageEvent == null)
        {
            _logger.LogWarning("PushNotificationConsumer: invalid payload");
            return;
        }

        var offlineRecipients = new List<Guid>();
        foreach (var recipientId in messageEvent.RecipientIds.Distinct())
        {
            if (await _presence.IsUserOnlineAsync(recipientId, ct))
                continue;
            offlineRecipients.Add(recipientId);
        }

        if (offlineRecipients.Count == 0)
            return;

        // Simple push content; in production customize per message type
        var title = "New message";
        var body = messageEvent.MessageType == "Text" ? "You have a new message" : "You received a media message";

        try
        {
            await _push.SendToUsersAsync(offlineRecipients, title, body, null, ct);
            _logger.LogInformation("Sent push notifications to {Count} users", offlineRecipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notifications");
        }
    }
}
