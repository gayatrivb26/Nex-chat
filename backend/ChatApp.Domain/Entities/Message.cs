using ChatApp.Domain.Enums;
using ChatApp.Domain.Events;

namespace ChatApp.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; private set; }
    public Guid? SenderId { get; private set; }
    public string? Content { get; private set; }
    public byte[]? EncryptedContent { get; private set; }   // E2EE payload
    public MessageType MessageType { get; private set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? MediaDuration { get; set; }
    public string? MimeType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public Guid? ForwardedFromMessageId { get; set; }
    public bool IsEdited { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool DeleteForEveryone { get; private set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Conversation? Conversation { get; set; }
    public User? Sender { get; set; }
    public Message? ReplyToMessage { get; set; }
    public ICollection<MessageStatus> Statuses { get; set; } = new List<MessageStatus>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();

    private Message() { }

    public static Message Create(
        Guid conversationId,
        Guid senderId,
        MessageType type,
        string? content = null,
        byte[]? encryptedContent = null,
        Guid? replyToMessageId = null)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            MessageType = type,
            Content = content,
            EncryptedContent = encryptedContent,
            ReplyToMessageId = replyToMessageId
        };
        message.AddDomainEvent(new MessageSentEvent(
            message.Id, conversationId, senderId, type, DateTime.UtcNow));
        return message;
    }

    public static Message CreateSystem(Guid conversationId, string content)
        => new()
        {
            ConversationId = conversationId,
            SenderId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MessageType = MessageType.System,
            Content = content
        };

    public void Edit(string newContent)
    {
        if (IsDeleted) throw new InvalidOperationException("Cannot edit a deleted message.");
        Content = newContent;
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
        AddDomainEvent(new MessageEditedEvent(Id, ConversationId, newContent));
    }

    public void DeleteForSender()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeleteForEveryone = false;
    }

    public void DeleteForAll()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeleteForEveryone = true;
        Content = null;
        EncryptedContent = null;
        MediaUrl = null;
        AddDomainEvent(new MessageDeletedEvent(Id, ConversationId, true));
    }

    public void SetMedia(string mediaUrl, string? thumbnailUrl, string? fileName,
        long? fileSize, string? mimeType, int? duration = null)
    {
        MediaUrl = mediaUrl;
        ThumbnailUrl = thumbnailUrl;
        FileName = fileName;
        FileSize = fileSize;
        MimeType = mimeType;
        MediaDuration = duration;
    }
}