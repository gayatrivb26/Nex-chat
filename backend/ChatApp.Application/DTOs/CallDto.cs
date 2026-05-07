using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record CallDto(
	Guid Id, Guid ConversationId, Guid InitiatorId,
	UserProfileDto? Initiator, string CallType, string Status,
	DateTime StartedAt, DateTime? AnsweredAt, DateTime? EndedAt, int DurationSeconds);
