using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class KeyBundle : BaseEntity
{
    public Guid UserId { get; private set; }
    public string IdentityKey { get; set; } = string.Empty;
    public int SignedPreKeyId { get; set; }
    public string SignedPreKey { get; set; } = string.Empty;
    public string SignedPreKeySig { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    private KeyBundle() { }

    public static KeyBundle Create(Guid userId, string identityKey,
        int signedPreKeyId, string signedPreKey, string signedPreKeySig)
        => new()
        {
            UserId = userId,
            IdentityKey = identityKey,
            SignedPreKeyId = signedPreKeyId,
            SignedPreKey = signedPreKey,
            SignedPreKeySig = signedPreKeySig
        };
}
