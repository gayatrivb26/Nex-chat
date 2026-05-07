using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public record UpdateGroupCommand(
    Guid UserId, Guid ConversationId,
    string? Name, string? Description, string? AvatarUrl) : IRequest<ConversationDto>;
