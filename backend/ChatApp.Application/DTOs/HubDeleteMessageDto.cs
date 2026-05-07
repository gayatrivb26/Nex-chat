using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record HubDeleteMessageDto(Guid MessageId, bool ForEveryone);
