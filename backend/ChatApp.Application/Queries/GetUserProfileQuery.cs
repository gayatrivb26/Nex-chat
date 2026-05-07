using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public record GetUserProfileQuery(Guid RequestingUserId, Guid TargetUserId) : IRequest<UserProfileDto>;
