using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record ReactionDto(Guid MessageId, Guid UserId, string? UserDisplayName, string? UserAvatarUrl, string Emoji, DateTime CreatedAt);
