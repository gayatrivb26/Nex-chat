using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class UserContact : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ContactUserId { get; private set; }
    public string? Nickname { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? BlockedAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public User? ContactUser { get; set; }

    private UserContact() { }

    public static UserContact Create(Guid userId, Guid contactUserId, string? nickname = null)
        => new() { UserId = userId, ContactUserId = contactUserId, Nickname = nickname };

    public void Block() { IsBlocked = true; BlockedAt = DateTime.UtcNow; }
    public void Unblock() { IsBlocked = false; BlockedAt = null; }
}
