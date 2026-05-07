using ChatApp.Application.DTOs;

namespace ChatApp.Application.Interfaces;

public interface IChatMessageService
{
    Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto, CancellationToken ct = default);
    Task<MessageDto> EditMessageAsync(Guid userId, EditMessageDto dto, CancellationToken ct = default);
    Task DeleteMessageAsync(Guid userId, DeleteMessageDto dto, CancellationToken ct = default);
    Task MarkMessagesReadAsync(Guid userId, Guid conversationId, Guid lastReadMessageId, CancellationToken ct = default);
    Task<ReactionDto> ReactToMessageAsync(Guid userId, ReactMessageDto dto, CancellationToken ct = default);
    Task RemoveReactionAsync(Guid userId, Guid messageId, string emoji, CancellationToken ct = default);
}
