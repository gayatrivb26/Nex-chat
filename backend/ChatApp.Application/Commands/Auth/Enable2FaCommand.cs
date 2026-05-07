using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public record Enable2FaCommand(Guid UserId, string TotpCode) : IRequest<string[]>; // returns backup codes
