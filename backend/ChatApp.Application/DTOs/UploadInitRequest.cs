using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UploadInitRequest(string FileName, string ContentType, long FileSize, string FileType);
