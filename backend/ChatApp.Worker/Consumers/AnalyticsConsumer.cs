using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ChatApp.Worker.Consumers;

public class AnalyticsConsumer
{
    private readonly ILogger<AnalyticsConsumer> _logger;

    public AnalyticsConsumer(ILogger<AnalyticsConsumer> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(string key, string payload, CancellationToken ct = default)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageSentEvent>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (messageEvent == null)
        {
            _logger.LogWarning("AnalyticsConsumer: invalid payload");
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Analytics event message.sent MessageId={MessageId} ConversationId={ConversationId} Type={MessageType} HasMedia={HasMedia} Recipients={RecipientCount}",
            messageEvent.MessageId,
            messageEvent.ConversationId,
            messageEvent.MessageType,
            messageEvent.HasMedia,
            messageEvent.RecipientIds.Count);

        return Task.CompletedTask;
    }
}
