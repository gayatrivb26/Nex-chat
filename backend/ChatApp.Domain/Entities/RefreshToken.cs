using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByToken { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User? User { get; set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, Guid familyId,
        int expiryDays, string? deviceName = null, string? deviceType = null,
        string? ip = null, string? userAgent = null)
        => new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            DeviceName = deviceName,
            DeviceType = deviceType,
            IpAddress = ip,
            UserAgent = userAgent
        };

    public bool IsValid() => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    public void Revoke(Guid? replacedBy = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedBy;
    }
}
