using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record NotificationDto(
	Guid Id, string Type, string? Title, string? Body,
	string? ImageUrl, bool IsRead, DateTime CreatedAt);
