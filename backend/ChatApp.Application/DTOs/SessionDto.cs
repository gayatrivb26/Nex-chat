
namespace ChatApp.Application.DTOs;

public record SessionDto(Guid Id, string? DeviceName, string? DeviceType, string? IpAddress, DateTime CreatedAt, DateTime ExpiresAt);