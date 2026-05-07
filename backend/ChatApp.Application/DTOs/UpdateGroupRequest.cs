using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UpdateGroupRequest(
	Guid ConversationId,
	string? Name,
	string? Description,
	string? AvatarUrl);
