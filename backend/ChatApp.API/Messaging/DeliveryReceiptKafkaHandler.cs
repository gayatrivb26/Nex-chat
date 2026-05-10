using System.Text.Json;
using ChatApp.API.Hubs;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.API.Messaging;

public sealed class DeliveryReceiptKafkaHandler(
    IUnitOfWork uow,
    IPresenceService presence,
    IHubContext<ChatHub> hubContext,
    ILogger<DeliveryReceiptKafkaHandler> logger) : IKafkaMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Topic => "messages.sent";
    public string GroupIdSuffix => "delivery-receipts";

    public async Task HandleAsync(string key, string payload, CancellationToken ct)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageSentKafkaEvent>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Invalid messages.sent payload.");

        var deliveredRecipients = new List<Guid>();
        foreach (var recipientId in messageEvent.RecipientIds.Distinct())
        {
            if (!await presence.IsUserOnlineAsync(recipientId, ct))
                continue;

            await uow.Messages.BulkUpdateStatusAsync(
                new[] { messageEvent.MessageId },
                recipientId,
                MessageStatusType.Delivered,
                ct);
            deliveredRecipients.Add(recipientId);
        }

        if (deliveredRecipients.Count == 0)
            return;

        var deliveredAt = DateTime.UtcNow;
        foreach (var recipientId in deliveredRecipients)
        {
            var update = new
            {
                MessageId = messageEvent.MessageId,
                UserId = recipientId,
                Status = MessageStatusType.Delivered.ToString(),
                DeliveredAt = deliveredAt,
                ReadAt = (DateTime?)null
            };

            await hubContext.Clients.Group(UserGroup(messageEvent.SenderId))
                .SendAsync("MessageStatusUpdated", update, ct);
            await hubContext.Clients.Group(ConversationGroup(messageEvent.ConversationId))
                .SendAsync("MessageStatusUpdated", update, ct);
        }

        logger.LogDebug("Marked message {MessageId} delivered for {RecipientCount} recipients",
            messageEvent.MessageId, deliveredRecipients.Count);
    }

    private static string UserGroup(Guid userId) => $"user:{userId}";
    private static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";
}
