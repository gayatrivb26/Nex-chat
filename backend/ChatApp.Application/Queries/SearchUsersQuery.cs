using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public record SearchUsersQuery(Guid RequestingUserId, string Query, int Limit = 20) : IRequest<IEnumerable<UserProfileDto>>;
