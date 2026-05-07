using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;

namespace ChatApp.Application.Services;

public class ChatMessageService(
    IUnitOfWork uow,
    ICacheService cache,
    IKafkaProducer kafkaProducer) : IChatMessageService
{
    public async Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto, CancellationToken ct = default)
    {
        var conversation = await uow.Conversations.GetWithMembersAsync(dto.ConversationId, ct)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var member = conversation.Members.FirstOrDefault(m => m.UserId == senderId);
        if (member == null || !member.CanSendMessages())
            throw new UnauthorizedAccessException("Cannot send messages to this conversation.");

        if (!Enum.TryParse<MessageType>(dto.MessageType, true, out var messageType))
            throw new InvalidOperationException("Invalid message type.");

        var message = Message.Create(dto.ConversationId, senderId, messageType,
            dto.Content, dto.EncryptedContent, dto.ReplyToMessageId);

        if (!string.IsNullOrWhiteSpace(dto.MediaUrl))
            message.SetMedia(dto.MediaUrl, null, dto.FileName, dto.FileSize, dto.MimeType);

        foreach (var recipient in conversation.Members.Where(m => m.UserId != senderId && m.IsActive()))
            message.Statuses.Add(MessageStatus.Create(message.Id, recipient.UserId));

        conversation.LastMessageId = message.Id;
        conversation.LastActivityAt = DateTime.UtcNow;

        await uow.Messages.AddAsync(message, ct);
        uow.Conversations.Update(conversation);
        await uow.SaveChangesAsync(ct);

        await cache.RemoveAsync($"conversation:{dto.ConversationId}:messages", ct);
        await cache.RemoveAsync($"user:{senderId}:conversations", ct);

        var mapped = MapMessage(message);
        await kafkaProducer.PublishAsync("messages.sent", dto.ConversationId.ToString(), mapped, ct);

        return mapped;
    }

    public async Task<MessageDto> EditMessageAsync(Guid userId, EditMessageDto dto, CancellationToken ct = default)
    {
        var message = await uow.Messages.GetByIdAsync(dto.MessageId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("Cannot edit another user's message.");
        if (message.MessageType != MessageType.Text)
            throw new InvalidOperationException("Only text messages can be edited.");
        if ((DateTime.UtcNow - message.CreatedAt).TotalHours > 24)
            throw new InvalidOperationException("Cannot edit messages older than 24 hours.");

        message.Edit(dto.NewContent);
        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"conversation:{message.ConversationId}:messages", ct);

        var mapped = MapMessage(message);
        await kafkaProducer.PublishAsync("messages.edited", message.ConversationId.ToString(), mapped, ct);
        return mapped;
    }

    public async Task DeleteMessageAsync(Guid userId, DeleteMessageDto dto, CancellationToken ct = default)
    {
        var message = await uow.Messages.GetByIdAsync(dto.MessageId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        if (dto.ForEveryone && message.SenderId != userId)
            throw new UnauthorizedAccessException("Only the sender can delete for everyone.");

        if (dto.ForEveryone) message.DeleteForAll();
        else message.DeleteForSender();

        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"conversation:{message.ConversationId}:messages", ct);
        await kafkaProducer.PublishAsync("messages.deleted", message.ConversationId.ToString(),
            new { message.Id, dto.ForEveryone }, ct);
    }

    public async Task MarkMessagesReadAsync(Guid userId, Guid conversationId, Guid lastReadMessageId, CancellationToken ct = default)
    {
        var member = await uow.Conversations.GetMemberAsync(conversationId, userId, ct);
        if (member == null) return;

        member.LastReadMessageId = lastReadMessageId;
        member.LastReadAt = DateTime.UtcNow;

        await uow.Messages.BulkUpdateStatusAsync([lastReadMessageId], userId, MessageStatusType.Read, ct);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"user:{userId}:unread_count", ct);
        await kafkaProducer.PublishAsync("messages.status", conversationId.ToString(),
            new { ConversationId = conversationId, UserId = userId, LastReadMessageId = lastReadMessageId }, ct);
    }

    public async Task<ReactionDto> ReactToMessageAsync(Guid userId, ReactMessageDto dto, CancellationToken ct = default)
    {
        var message = await uow.Messages.GetWithStatusAsync(dto.MessageId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        var existing = message.Reactions.FirstOrDefault(r => r.UserId == userId && r.Emoji == dto.Emoji);
        if (existing != null)
            return new ReactionDto(dto.MessageId, userId, null, null, dto.Emoji, existing.CreatedAt);

        var reaction = MessageReaction.Create(dto.MessageId, userId, dto.Emoji);
        message.Reactions.Add(reaction);
        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);

        var mapped = new ReactionDto(dto.MessageId, userId, null, null, dto.Emoji, reaction.CreatedAt);
        await kafkaProducer.PublishAsync("messages.reactions", message.ConversationId.ToString(), mapped, ct);
        return mapped;
    }

    public async Task RemoveReactionAsync(Guid userId, Guid messageId, string emoji, CancellationToken ct = default)
    {
        var message = await uow.Messages.GetWithStatusAsync(messageId, ct);
        if (message == null) return;

        var reaction = message.Reactions.FirstOrDefault(r => r.UserId == userId && r.Emoji == emoji);
        if (reaction == null) return;

        message.Reactions.Remove(reaction);
        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);
        await kafkaProducer.PublishAsync("messages.reactions", message.ConversationId.ToString(),
            new { MessageId = messageId, UserId = userId, Emoji = emoji, Removed = true }, ct);
    }

    private static MessageDto MapMessage(Message m)
    {
        var statuses = m.Statuses.Select(s =>
            new MessageStatusDto(s.UserId, s.Status.ToString(), s.DeliveredAt, s.ReadAt)).ToList();

        var reactions = m.Reactions.Select(r =>
            new ReactionDto(r.MessageId, r.UserId, r.User?.DisplayName, r.User?.AvatarUrl, r.Emoji, r.CreatedAt)).ToList();

        return new MessageDto(
            m.Id, m.ConversationId, m.SenderId, null,
            m.Content, m.MessageType.ToString(), m.MediaUrl, m.ThumbnailUrl,
            m.FileName, m.FileSize, m.MediaDuration, m.MimeType,
            m.ReplyToMessageId, null, m.IsEdited, m.IsDeleted,
            statuses, reactions, m.CreatedAt, m.EditedAt);
    }
}
