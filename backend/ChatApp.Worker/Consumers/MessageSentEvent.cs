using System.Text.Json.Serialization;

namespace ChatApp.Worker.Consumers;

public class MessageSentEvent
{
    [JsonPropertyName("MessageId")] public Guid MessageId { get; set; }
    [JsonPropertyName("ConversationId")] public Guid ConversationId { get; set; }
    [JsonPropertyName("SenderId")] public Guid SenderId { get; set; }
    [JsonPropertyName("MessageType")] public string? MessageType { get; set; }
    [JsonPropertyName("HasMedia")] public bool HasMedia { get; set; }
    [JsonPropertyName("RecipientIds")] public List<Guid> RecipientIds { get; set; } = new();
    [JsonPropertyName("SentAt")] public DateTime SentAt { get; set; }
}
