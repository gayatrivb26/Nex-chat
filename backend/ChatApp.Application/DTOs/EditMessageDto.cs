namespace ChatApp.Application.DTOs;

public record EditMessageDto(Guid MessageId, string NewContent);
