namespace ChatApp.Application.DTOs;

public record SendMessageDto(
    Guid ConversationId,
    string? Content,
    string MessageType = "text",
    Guid? ReplyToMessageId = null,
    string? MediaUrl = null,
    string? FileName = null,
    long? FileSize = null,
    string? MimeType = null,
    byte[]? EncryptedContent = null);
