using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UploadInitResponse(Guid FileId, string UploadUrl, string ObjectName, DateTime ExpiresAt);
