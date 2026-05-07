using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record RemoveMemberRequest(Guid ConversationId, Guid UserId);
