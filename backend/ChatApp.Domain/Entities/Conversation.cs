using ChatApp.Domain.Enums;
using ChatApp.Domain.Events;

namespace ChatApp.Domain.Entities;

public class Conversation : BaseEntity
{
    public ConversationType Type { get; private set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? CreatedById { get; private set; }
    public Guid? LastMessageId { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public User? CreatedBy { get; set; }
    public Message? LastMessage { get; set; }
    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();

    private Conversation() { }

    public static Conversation CreatePrivate(Guid createdById, Guid otherUserId)
    {
        var conversation = new Conversation
        {
            Type = ConversationType.Private,
            CreatedById = createdById
        };
        conversation.Members.Add(ConversationMember.Create(conversation.Id, createdById, MemberRole.Member));
        conversation.Members.Add(ConversationMember.Create(conversation.Id, otherUserId, MemberRole.Member));
        conversation.AddDomainEvent(new ConversationCreatedEvent(conversation.Id, ConversationType.Private, createdById));
        return conversation;
    }

    public static Conversation CreateGroup(Guid createdById, string name, string? description, List<Guid> memberIds)
    {
        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            Name = name,
            Description = description,
            CreatedById = createdById
        };
        conversation.Members.Add(ConversationMember.Create(conversation.Id, createdById, MemberRole.Owner));
        foreach (var memberId in memberIds)
            conversation.Members.Add(ConversationMember.Create(conversation.Id, memberId, MemberRole.Member));

        conversation.AddDomainEvent(new ConversationCreatedEvent(conversation.Id, ConversationType.Group, createdById));
        return conversation;
    }
}
