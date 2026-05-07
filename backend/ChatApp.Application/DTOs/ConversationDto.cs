using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record ConversationDto(
	Guid Id,
	string Type,
	string? Name,
	string? Description,
	string? AvatarUrl,
	MessageDto? LastMessage,
	int UnreadCount,
	DateTime LastActivityAt,
	List<ConversationMemberDto> Members);
