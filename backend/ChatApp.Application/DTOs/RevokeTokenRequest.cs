using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record RevokeTokenRequest(string RefreshToken);
