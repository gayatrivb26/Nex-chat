using ChatApp.Domain.Enums;
using ChatApp.Domain.Events;

namespace ChatApp.Domain.Entities;

public class User : AuditableEntity
{
    public string Username { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Offline;
    public DateTime? LastSeen { get; set; }
    public bool IsVerified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string[]? BackupCodes { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    // E2EE Signal Protocol keys
    public string? IdentityPublicKey { get; set; }
    public string? SignedPreKeyId { get; set; }
    public string? SignedPreKey { get; set; }
    public string? SignedPreKeySignature { get; set; }

    // Navigation
    public ICollection<ConversationMember> ConversationMemberships { get; set; } = new List<ConversationMember>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    private User() { }

    public static User Create(string username, string phone, string passwordHash, string? email = null)
    {
        var user = new User
        {
            Username = username.ToLowerInvariant().Trim(),
            Phone = phone.Trim(),
            PasswordHash = passwordHash,
            Email = email?.ToLowerInvariant().Trim(),
            DisplayName = username
        };
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Phone, user.Email));
        return user;
    }

    public void UpdateProfile(string? displayName, string? bio, string? avatarUrl)
    {
        DisplayName = displayName ?? DisplayName;
        Bio = bio ?? Bio;
        AvatarUrl = avatarUrl ?? AvatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetOnline()
    {
        Status = UserStatus.Online;
        LastSeen = DateTime.UtcNow;
    }

    public void SetOffline()
    {
        Status = UserStatus.Offline;
        LastSeen = DateTime.UtcNow;
    }

    public void MarkVerified()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 10)
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Username = $"deleted_{Id:N}";
        Email = null;
        Phone = $"+00000{Id:N}";
        DisplayName = "Deleted User";
        Bio = null;
        AvatarUrl = null;
        TwoFactorSecret = null;
        BackupCodes = null;
        AddDomainEvent(new UserDeletedEvent(Id));
    }

    public void UpdateE2EEKeys(string identityPublicKey, string signedPreKeyId, string signedPreKey, string signature)
    {
        IdentityPublicKey = identityPublicKey;
        SignedPreKeyId = signedPreKeyId;
        SignedPreKey = signedPreKey;
        SignedPreKeySignature = signature;
        UpdatedAt = DateTime.UtcNow;
    }
}