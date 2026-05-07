using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Events;

public record MediaScannedEvent(Guid FileId, bool IsClean, string ScanResult);
