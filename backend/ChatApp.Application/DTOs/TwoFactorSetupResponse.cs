using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record TwoFactorSetupResponse(
	string Secret,
	string QrCodeUri,
	string[] BackupCodes);
