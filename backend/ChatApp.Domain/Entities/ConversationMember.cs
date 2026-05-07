using ChatApp.Domain.Enums;
using ChatApp.Domain.Events;

namespace ChatApp.Domain.Entities;

public class ConversationMember : BaseEntity
{
    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }
    public MemberRole Role { get; set; } = MemberRole.Member;
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsMuted { get; set; }
    public DateTime? MuteUntil { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTime? LastReadAt { get; set; }
    public bool NotificationsEnabled { get; set; } = true;

    // Navigation
    public Conversation? Conversation { get; set; }
    public User? User { get; set; }

    private ConversationMember() { }

    public static ConversationMember Create(Guid conversationId, Guid userId, MemberRole role = MemberRole.Member)
        => new() { ConversationId = conversationId, UserId = userId, Role = role };

    public bool IsActive() => LeftAt == null;
    public bool CanSendMessages() => IsActive() && (!IsMuted || (MuteUntil.HasValue && MuteUntil < DateTime.UtcNow));
    public bool CanManageMembers() => IsActive() && Role is MemberRole.Admin or MemberRole.Owner;
    public bool IsOwner() => IsActive() && Role == MemberRole.Owner;
}