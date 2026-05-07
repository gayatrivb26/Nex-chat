using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record ContactDto(
	Guid Id, Guid ContactUserId, string? Nickname,
	UserProfileDto? ContactUser, bool IsBlocked, DateTime CreatedAt);
