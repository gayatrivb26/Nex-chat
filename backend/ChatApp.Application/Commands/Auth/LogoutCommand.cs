using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public record LogoutCommand(Guid UserId, string? RefreshToken, string Jti, TimeSpan JtiTtl) : IRequest;
