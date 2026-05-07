using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record InitiateCallRequest(Guid ConversationId, string CallType);
