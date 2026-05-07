using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public record AddMembersCommand(Guid UserId, Guid ConversationId, List<Guid> NewMemberIds) : IRequest;
