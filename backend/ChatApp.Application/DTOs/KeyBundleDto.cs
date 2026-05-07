using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record KeyBundleDto(
	Guid UserId,
	string IdentityKey,
	int SignedPreKeyId,
	string SignedPreKey,
	string SignedPreKeySig,
	OneTimePreKeyDto? OneTimePreKey);
