using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User? User { get; set; }

    private AuditLog() { }

    public static AuditLog Create(string action, Guid? userId = null,
        string? entityType = null, Guid? entityId = null,
        string? oldValues = null, string? newValues = null,
        string? ip = null, string? userAgent = null)
        => new()
        {
            Action = action,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ip,
            UserAgent = userAgent
        };
}
