using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public record SendMessageCommand(
    Guid SenderId, Guid ConversationId, string? Content,
    MessageType MessageType, Guid? ReplyToMessageId,
    string? MediaUrl, string? FileName, long? FileSize,
    string? MimeType, byte[]? EncryptedContent) : IRequest<MessageDto>;
