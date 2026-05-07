using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record ConversationMemberDto(
	Guid UserId,
	string? DisplayName,
	string? AvatarUrl,
	string Role,
	bool IsMuted,
	DateTime JoinedAt,
	string Status);
