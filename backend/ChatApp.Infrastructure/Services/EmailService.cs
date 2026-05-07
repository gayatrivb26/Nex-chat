using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendOtpEmailAsync(string email, string otp, CancellationToken ct = default)
    {
        logger.LogInformation("OTP email queued for {Email}", email);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken ct = default)
    {
        logger.LogInformation("Welcome email queued for {Email}", email);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation("Password reset email queued for {Email}", email);
        return Task.CompletedTask;
    }

    public Task SendSecurityAlertEmailAsync(string email, string alertMessage, CancellationToken ct = default)
    {
        logger.LogInformation("Security alert email queued for {Email}", email);
        return Task.CompletedTask;
    }
}
