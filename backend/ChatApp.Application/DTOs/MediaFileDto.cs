using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record MediaFileDto(
	Guid Id, string OriginalName, string? MimeType, long? FileSize,
	string? MediaUrl, string? ThumbnailUrl, int? Width, int? Height,
	int? Duration, DateTime CreatedAt);
