using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class OneTimePreKey : BaseEntity
{
    public Guid UserId { get; private set; }
    public int KeyId { get; private set; }
    public string PublicKey { get; private set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User? User { get; set; }

    private OneTimePreKey() { }

    public static OneTimePreKey Create(Guid userId, int keyId, string publicKey)
        => new() { UserId = userId, KeyId = keyId, PublicKey = publicKey };

    public void MarkUsed() { IsUsed = true; UsedAt = DateTime.UtcNow; }
}
