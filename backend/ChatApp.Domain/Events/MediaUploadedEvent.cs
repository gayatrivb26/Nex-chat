using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MediaUploadedEvent(Guid FileId, Guid UploadedById, string FilePath, string MimeType);
