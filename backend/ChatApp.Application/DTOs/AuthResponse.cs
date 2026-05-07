using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record AuthResponse(
	string AccessToken,
	string RefreshToken,
	DateTime AccessTokenExpiry,
	UserDto User);
