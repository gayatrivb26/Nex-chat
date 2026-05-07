using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UserDto(
	Guid Id,
	string Username,
	string? Email,
	string Phone,
	string? AvatarUrl,
	string? DisplayName,
	string? Bio,
	string Status,
	DateTime? LastSeen,
	bool IsVerified,
	bool TwoFactorEnabled,
	DateTime CreatedAt);
