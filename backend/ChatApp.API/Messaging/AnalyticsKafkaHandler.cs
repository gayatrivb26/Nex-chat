using System.Text.Json;

namespace ChatApp.API.Messaging;

public sealed class AnalyticsKafkaHandler(ILogger<AnalyticsKafkaHandler> logger) : IKafkaMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Topic => "messages.sent";
    public string GroupIdSuffix => "analytics";

    public Task HandleAsync(string key, string payload, CancellationToken ct)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageSentKafkaEvent>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Invalid messages.sent payload.");

        logger.LogInformation(
            "Analytics event message.sent MessageId={MessageId} ConversationId={ConversationId} Type={MessageType} HasMedia={HasMedia} Recipients={RecipientCount}",
            messageEvent.MessageId,
            messageEvent.ConversationId,
            messageEvent.MessageType,
            messageEvent.HasMedia,
            messageEvent.RecipientIds.Count);

        return Task.CompletedTask;
    }
}
