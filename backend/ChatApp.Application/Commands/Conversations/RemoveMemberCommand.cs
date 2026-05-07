using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Commands.Conversations;

public record RemoveMemberCommand(Guid RequestingUserId, Guid ConversationId, Guid TargetUserId) : IRequest;
