namespace ChatApp.Application.DTOs;

public record DeleteMessageDto(Guid MessageId, bool ForEveryone = false);
