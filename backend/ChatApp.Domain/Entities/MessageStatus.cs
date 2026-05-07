using ChatApp.Domain.Enums;
using ChatApp.Domain.Events;

namespace ChatApp.Domain.Entities;

public class MessageStatus : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public MessageStatusType Status { get; set; } = MessageStatusType.Sent;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public Message? Message { get; set; }
    public User? User { get; set; }

    private MessageStatus() { }

    public static MessageStatus Create(Guid messageId, Guid userId)
        => new() { MessageId = messageId, UserId = userId, Status = MessageStatusType.Sent };

    public void MarkDelivered()
    {
        if (Status == MessageStatusType.Sent)
        {
            Status = MessageStatusType.Delivered;
            DeliveredAt = DateTime.UtcNow;
        }
    }

    public void MarkRead()
    {
        Status = MessageStatusType.Read;
        ReadAt = DateTime.UtcNow;
        if (!DeliveredAt.HasValue) DeliveredAt = ReadAt;
    }
}