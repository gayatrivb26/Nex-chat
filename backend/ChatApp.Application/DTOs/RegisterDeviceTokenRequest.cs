namespace ChatApp.Application.DTOs;

public record RegisterDeviceTokenRequest(string FcmToken, string DeviceType);
