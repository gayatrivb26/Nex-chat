using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record MessageStatusDto(Guid UserId, string Status, DateTime? DeliveredAt, DateTime? ReadAt);
