using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? Title { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string>? Payload { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User? User { get; set; }

    private Notification() { }

    public static Notification Create(Guid userId, string type, string title, string body,
        Dictionary<string, string>? payload = null, string? imageUrl = null)
        => new()
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Payload = payload,
            ImageUrl = imageUrl
        };

    public void MarkRead() { IsRead = true; ReadAt = DateTime.UtcNow; }
}
