using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record ResetPasswordRequest(string Phone, string Otp, string NewPassword);
