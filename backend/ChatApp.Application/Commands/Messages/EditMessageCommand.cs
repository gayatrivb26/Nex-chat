using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Messages;

public record EditMessageCommand(Guid UserId, Guid MessageId, string NewContent) : IRequest<MessageDto>;
