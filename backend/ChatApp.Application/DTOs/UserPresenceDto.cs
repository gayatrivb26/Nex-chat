using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record UserPresenceDto(Guid UserId, string Status, DateTime? LastSeen);
