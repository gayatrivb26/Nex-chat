using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UserProfileDto(
	Guid Id,
	string Username,
	string? AvatarUrl,
	string? DisplayName,
	string? Bio,
	string Status,
	DateTime? LastSeen,
	bool IsVerified);
