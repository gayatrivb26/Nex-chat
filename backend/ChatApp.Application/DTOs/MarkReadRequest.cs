using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record MarkReadRequest(Guid ConversationId, Guid LastReadMessageId);
