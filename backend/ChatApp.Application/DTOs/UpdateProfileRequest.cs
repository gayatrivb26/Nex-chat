using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UpdateProfileRequest(
	string? DisplayName,
	string? Bio,
	string? AvatarUrl);
