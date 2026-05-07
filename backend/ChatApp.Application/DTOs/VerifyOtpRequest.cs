using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record VerifyOtpRequest(string Phone, string Otp);
