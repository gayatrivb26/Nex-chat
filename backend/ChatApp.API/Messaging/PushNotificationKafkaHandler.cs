using System.Text.Json;
using ChatApp.API.Hubs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.API.Messaging;

public sealed class PushNotificationKafkaHandler(
    IUnitOfWork uow,
    IPushNotificationService pushNotifications,
    IHubContext<ChatHub> hubContext,
    ILogger<PushNotificationKafkaHandler> logger) : IKafkaMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Topic => "messages.sent";
    public string GroupIdSuffix => "push-notifications";

    public async Task HandleAsync(string key, string payload, CancellationToken ct)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageSentKafkaEvent>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Invalid messages.sent payload.");

        var conversation = await uow.Conversations.GetWithMembersAsync(messageEvent.ConversationId, ct);
        if (conversation == null)
        {
            logger.LogWarning("Skipping notification for missing conversation {ConversationId}", messageEvent.ConversationId);
            return;
        }

        var sender = conversation.Members.FirstOrDefault(m => m.UserId == messageEvent.SenderId)?.User
            ?? await uow.Users.GetByIdAsync(messageEvent.SenderId, ct);
        var senderName = sender?.DisplayName ?? sender?.Username ?? "New message";
        var title = conversation.Type == ConversationType.Group && !string.IsNullOrWhiteSpace(conversation.Name)
            ? conversation.Name
            : senderName;
        var body = BuildNotificationBody(senderName, messageEvent);
        var recipientIds = conversation.Members
            .Where(m => m.LeftAt == null
                     && m.UserId != messageEvent.SenderId
                     && messageEvent.RecipientIds.Contains(m.UserId)
                     && m.NotificationsEnabled
                     && (!m.IsMuted || (m.MuteUntil.HasValue && m.MuteUntil <= DateTime.UtcNow)))
            .Select(m => m.UserId)
            .Distinct()
            .ToArray();

        if (recipientIds.Length == 0)
            return;

        var data = new Dictionary<string, string>
        {
            ["type"] = "message",
            ["messageId"] = messageEvent.MessageId.ToString(),
            ["conversationId"] = messageEvent.ConversationId.ToString(),
            ["senderId"] = messageEvent.SenderId.ToString()
        };

        var notifications = recipientIds
            .Select(recipientId => Notification.Create(
                recipientId,
                NotificationType.NewMessage.ToString(),
                title,
                body,
                data))
            .ToList();

        await uow.Notifications.AddRangeAsync(notifications, ct);
        await uow.SaveChangesAsync(ct);

        foreach (var notification in notifications)
        {
            await hubContext.Clients.Group(UserGroup(notification.UserId))
                .SendAsync("NotificationReceived", new
                {
                    notification.Id,
                    notification.Type,
                    notification.Title,
                    notification.Body,
                    notification.ImageUrl,
                    notification.IsRead,
                    notification.CreatedAt,
                    notification.Payload
                }, ct);
        }

        await pushNotifications.SendToUsersAsync(recipientIds, title, body, data, ct);
        logger.LogDebug("Created message notifications for {RecipientCount} recipients", recipientIds.Length);
    }

    private static string BuildNotificationBody(string senderName, MessageSentKafkaEvent messageEvent)
        => messageEvent.MessageType.ToLowerInvariant() switch
        {
            "text" => $"{senderName}: New message",
            "image" => $"{senderName} sent an image",
            "video" => $"{senderName} sent a video",
            "audio" or "voice" => $"{senderName} sent a voice message",
            "file" => $"{senderName} sent a file",
            _ => $"{senderName} sent a message"
        };

    private static string UserGroup(Guid userId) => $"user:{userId}";
}
