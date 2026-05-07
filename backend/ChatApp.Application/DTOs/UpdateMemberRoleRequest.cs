using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UpdateMemberRoleRequest(Guid ConversationId, Guid UserId, string Role);
