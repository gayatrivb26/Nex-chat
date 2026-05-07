using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MessageReactionAddedEvent(Guid MessageId, Guid UserId, string Emoji);
