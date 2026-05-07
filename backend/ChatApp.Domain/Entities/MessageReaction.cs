using ChatApp.Domain.Enums;
using ChatApp.Domain.Events;

namespace ChatApp.Domain.Entities;

public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Emoji { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Message? Message { get; set; }
    public User? User { get; set; }

    private MessageReaction() { }

    public static MessageReaction Create(Guid messageId, Guid userId, string emoji)
        => new() { MessageId = messageId, UserId = userId, Emoji = emoji };
}