namespace ChatApp.Application.DTOs;

public record InitiateCallDto(Guid ConversationId, Guid TargetUserId, string CallType);
