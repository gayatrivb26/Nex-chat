using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public record VerifyOtpCommand(string Phone, string Otp, string? DeviceName, string? DeviceType,
    string? IpAddress, string? UserAgent) : IRequest<AuthResponse>;
