using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record RegisterKeyBundleRequest(
	string IdentityKey,
	int SignedPreKeyId,
	string SignedPreKey,
	string SignedPreKeySig,
	List<OneTimePreKeyDto> OneTimePreKeys);
