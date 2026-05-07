using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record OneTimePreKeyDto(int KeyId, string PublicKey);
