using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data.Repositories;

namespace ChatApp.Application.Services;

public class ChatMessageService(
    IUnitOfWork uow,
    ICacheService cache,
    IKafkaProducer kafkaProducer) : IChatMessageService
{
    public async Task<MessageDto> SendMessageAsync(
        Guid senderId, SendMessageDto dto, CancellationToken ct = default)
    {
        var conversation = await uow.Conversations.GetWithMembersAsync(dto.ConversationId, ct)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var member = conversation.Members.FirstOrDefault(m => m.UserId == senderId && m.LeftAt == null);
        if (member == null || !member.CanSendMessages())
            throw new UnauthorizedAccessException("Cannot send messages to this conversation.");

        if (!Enum.TryParse<MessageType>(dto.MessageType, true, out var messageType))
            throw new InvalidOperationException($"Invalid message type: '{dto.MessageType}'.");

        var message = Message.Create(dto.ConversationId, senderId, messageType,
            dto.Content, dto.EncryptedContent, dto.ReplyToMessageId);

        if (!string.IsNullOrWhiteSpace(dto.MediaUrl))
            message.SetMedia(dto.MediaUrl, null, dto.FileName, dto.FileSize, dto.MimeType);

        // Create delivery status entries for all active recipients
        foreach (var recipient in conversation.Members.Where(m => m.UserId != senderId && m.LeftAt == null))
            message.Statuses.Add(MessageStatus.Create(message.Id, recipient.UserId));

        conversation.LastMessageId = message.Id;
        conversation.LastActivityAt = DateTime.UtcNow;

        await uow.Messages.AddAsync(message, ct);
        uow.Conversations.Update(conversation);
        await uow.SaveChangesAsync(ct);

        // Invalidate caches
        await cache.RemoveAsync($"conversation:{dto.ConversationId}:messages", ct);
        await cache.RemoveAsync($"user:{senderId}:conversations", ct);

        // Notify other members their cache is stale
        foreach (var m in conversation.Members.Where(m => m.UserId != senderId && m.LeftAt == null))
            await cache.RemoveAsync($"user:{m.UserId}:unread_count", ct);

        var mapped = MapMessage(message);

        // Publish to Kafka for async consumers (delivery receipts, push notifications, analytics)
        await kafkaProducer.PublishAsync("messages.sent", dto.ConversationId.ToString(), new
        {
            MessageId = message.Id,
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            MessageType = dto.MessageType,
            HasMedia = !string.IsNullOrWhiteSpace(dto.MediaUrl),
            RecipientIds = conversation.Members
                .Where(m => m.UserId != senderId && m.LeftAt == null)
                .Select(m => m.UserId).ToList(),
            SentAt = message.CreatedAt
        }, ct);

        return mapped;
    }

    public async Task<MessageDto> EditMessageAsync(
        Guid userId, EditMessageDto dto, CancellationToken ct = default)
    {
        var message = await uow.Messages.GetByIdAsync(dto.MessageId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("Cannot edit another user's message.");
        if (message.MessageType != MessageType.Text)
            throw new InvalidOperationException("Only text messages can be edited.");
        if (message.IsDeleted)
            throw new InvalidOperationException("Cannot edit a deleted message.");
        if ((DateTime.UtcNow - message.CreatedAt).TotalHours > 24)
            throw new InvalidOperationException("Cannot edit messages older than 24 hours.");

        message.Edit(dto.NewContent);
        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"conversation:{message.ConversationId}:messages", ct);

        var mapped = MapMessage(message);
        await kafkaProducer.PublishAsync("messages.edited", message.ConversationId.ToString(), new
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            NewContent = dto.NewContent,
            EditedAt = message.EditedAt
        }, ct);

        return mapped;
    }

    public async Task DeleteMessageAsync(
        Guid userId, DeleteMessageDto dto, CancellationToken ct = default)
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

        await kafkaProducer.PublishAsync("messages.deleted", message.ConversationId.ToString(), new
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            ForEveryone = dto.ForEveryone,
            DeletedAt = DateTime.UtcNow
        }, ct);
    }

    public async Task MarkMessagesReadAsync(
        Guid userId, Guid conversationId, Guid lastReadMessageId, CancellationToken ct = default)
    {
        var member = await uow.Conversations.GetMemberAsync(conversationId, userId, ct);
        if (member == null) return;

        // Get the timestamp of the last-read message to bulk-update all prior messages
        var lastReadMsg = await uow.Messages.GetByIdAsync(lastReadMessageId, ct);
        if (lastReadMsg == null) return;

        member.LastReadMessageId = lastReadMessageId;
        member.LastReadAt = DateTime.UtcNow;

        // Bulk-mark all messages up to lastReadMessageId as read
        if (uow.Messages is MessageRepository repo)
            await repo.BulkMarkReadUpToAsync(conversationId, userId, lastReadMsg.CreatedAt, ct);
        else
            await uow.Messages.BulkUpdateStatusAsync([lastReadMessageId], userId, MessageStatusType.Read, ct);

        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync($"user:{userId}:unread_count", ct);

        await kafkaProducer.PublishAsync("messages.status", conversationId.ToString(), new
        {
            ConversationId = conversationId,
            UserId = userId,
            LastReadMessageId = lastReadMessageId,
            ReadAt = DateTime.UtcNow
        }, ct);
    }

    public async Task<ReactionDto> ReactToMessageAsync(
        Guid userId, ReactMessageDto dto, CancellationToken ct = default)
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
        await kafkaProducer.PublishAsync("messages.reactions", message.ConversationId.ToString(), new
        {
            MessageId = dto.MessageId,
            UserId = userId,
            Emoji = dto.Emoji,
            Removed = false,
            ConversationId = message.ConversationId
        }, ct);
        return mapped;
    }

    public async Task RemoveReactionAsync(
        Guid userId, Guid messageId, string emoji, CancellationToken ct = default)
    {
        var message = await uow.Messages.GetWithStatusAsync(messageId, ct);
        if (message == null) return;

        var reaction = message.Reactions.FirstOrDefault(r => r.UserId == userId && r.Emoji == emoji);
        if (reaction == null) return;

        message.Reactions.Remove(reaction);
        uow.Messages.Update(message);
        await uow.SaveChangesAsync(ct);

        await kafkaProducer.PublishAsync("messages.reactions", message.ConversationId.ToString(), new
        {
            MessageId = messageId,
            UserId = userId,
            Emoji = emoji,
            Removed = true,
            ConversationId = message.ConversationId
        }, ct);
    }

    private static MessageDto MapMessage(Message m)
    {
        var statuses = m.Statuses
            .Select(s => new MessageStatusDto(s.UserId, s.Status.ToString(), s.DeliveredAt, s.ReadAt))
            .ToList();

        var reactions = m.Reactions
            .Select(r => new ReactionDto(r.MessageId, r.UserId,
                r.User?.DisplayName, r.User?.AvatarUrl, r.Emoji, r.CreatedAt))
            .ToList();

        return new MessageDto(
            m.Id, m.ConversationId, m.SenderId, null,
            m.IsDeleted && m.DeleteForEveryone ? null : m.Content,
            m.MessageType.ToString(),
            m.IsDeleted && m.DeleteForEveryone ? null : m.MediaUrl,
            m.ThumbnailUrl, m.FileName, m.FileSize, m.MediaDuration, m.MimeType,
            m.ReplyToMessageId, null, m.IsEdited, m.IsDeleted,
            statuses, reactions, m.CreatedAt, m.EditedAt);
    }
}