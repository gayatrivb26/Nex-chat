using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record DeleteMessageRequest(Guid MessageId, bool ForEveryone = false);
