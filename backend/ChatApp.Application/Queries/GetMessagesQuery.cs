using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public record GetMessagesQuery(Guid UserId, Guid ConversationId, int Take = 50, DateTime? Before = null)
    : IRequest<IEnumerable<MessageDto>>;
