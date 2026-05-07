using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record MessageDto(
	Guid Id,
	Guid ConversationId,
	Guid? SenderId,
	UserProfileDto? Sender,
	string? Content,
	string MessageType,
	string? MediaUrl,
	string? ThumbnailUrl,
	string? FileName,
	long? FileSize,
	int? MediaDuration,
	string? MimeType,
	Guid? ReplyToMessageId,
	MessageDto? ReplyToMessage,
	bool IsEdited,
	bool IsDeleted,
	List<MessageStatusDto> Statuses,
	List<ReactionDto> Reactions,
	DateTime CreatedAt,
	DateTime? EditedAt);
