using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record CreatePrivateConversationRequest(Guid OtherUserId);
