using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record HubEditMessageDto(Guid MessageId, string NewContent);
