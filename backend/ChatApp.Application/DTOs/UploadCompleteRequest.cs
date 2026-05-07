using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UploadCompleteRequest(Guid FileId, string ObjectName);
