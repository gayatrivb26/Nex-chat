using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MessageReactionRemovedEvent(Guid MessageId, Guid UserId, string Emoji);
