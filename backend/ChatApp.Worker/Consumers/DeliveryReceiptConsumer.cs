using System.Text.Json;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Worker.Consumers;

public class DeliveryReceiptConsumer
{
    private readonly IUnitOfWork _uow;
    private readonly IPresenceService _presence;
    private readonly ILogger<DeliveryReceiptConsumer> _logger;

    public DeliveryReceiptConsumer(IUnitOfWork uow, IPresenceService presence, ILogger<DeliveryReceiptConsumer> logger)
    {
        _uow = uow;
        _presence = presence;
        _logger = logger;
    }

    public async Task HandleAsync(string key, string payload, CancellationToken ct = default)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageSentEvent>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (messageEvent == null)
        {
            _logger.LogWarning("DeliveryReceiptConsumer: invalid payload");
            return;
        }

        var deliveredRecipients = new List<Guid>();
        foreach (var recipientId in messageEvent.RecipientIds.Distinct())
        {
            if (!await _presence.IsUserOnlineAsync(recipientId, ct))
                continue;

            await _uow.Messages.BulkUpdateStatusAsync(
                new[] { messageEvent.MessageId },
                recipientId,
                MessageStatusType.Delivered,
                ct);

            deliveredRecipients.Add(recipientId);
        }

        if (deliveredRecipients.Count == 0)
            return;

        _logger.LogDebug("Marked message {MessageId} delivered for {RecipientCount} recipients",
            messageEvent.MessageId, deliveredRecipients.Count);
    }
}
