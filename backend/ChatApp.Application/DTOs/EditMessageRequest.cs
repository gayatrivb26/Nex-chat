using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record EditMessageRequest(Guid MessageId, string NewContent);
