using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record LoginRequest(string Phone, string Password, string? TotpCode = null,
	string? DeviceName = null, string? DeviceType = null);
