using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
