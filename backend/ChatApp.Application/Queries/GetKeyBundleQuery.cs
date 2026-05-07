using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public record GetKeyBundleQuery(Guid RequestingUserId, Guid TargetUserId) : IRequest<KeyBundleDto>;
