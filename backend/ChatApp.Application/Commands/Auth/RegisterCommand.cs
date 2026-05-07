using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public record RegisterCommand(string Username, string Phone, string Password, string? Email) : IRequest<AuthResponse>;
