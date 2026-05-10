namespace ChatApp.API.Messaging;

public sealed record MessageSentKafkaEvent
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderId { get; init; }
    public string MessageType { get; init; } = "text";
    public bool HasMedia { get; init; }
    public IReadOnlyList<Guid> RecipientIds { get; init; } = Array.Empty<Guid>();
    public DateTime SentAt { get; init; }
}
