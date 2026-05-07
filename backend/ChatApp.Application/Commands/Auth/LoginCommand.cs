using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public record LoginCommand(string Phone, string Password, string? TotpCode,
    string? DeviceName, string? DeviceType, string? IpAddress, string? UserAgent) : IRequest<AuthResponse>;
