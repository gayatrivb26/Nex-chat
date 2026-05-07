using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public record CreateGroupCommand(
    Guid CreatorId, string Name, string? Description,
    List<Guid> MemberIds, string? AvatarUrl) : IRequest<ConversationDto>;
