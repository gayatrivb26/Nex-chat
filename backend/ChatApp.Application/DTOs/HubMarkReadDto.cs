using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record HubMarkReadDto(Guid ConversationId, Guid LastReadMessageId);
