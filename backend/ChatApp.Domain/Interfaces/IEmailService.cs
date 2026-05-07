using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string otp, CancellationToken ct = default);
    Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken ct = default);
    Task SendSecurityAlertEmailAsync(string email, string alertMessage, CancellationToken ct = default);
}
