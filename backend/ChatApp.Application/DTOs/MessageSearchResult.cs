using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record MessageSearchResult(
	Guid Id, Guid ConversationId, string? Content,
	UserProfileDto? Sender, DateTime CreatedAt);
