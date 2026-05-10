using ChatApp.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ChatApp.Infrastructure.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public Task SendOtpEmailAsync(string email, string otp, CancellationToken ct = default)
    {
        return SendAsync(email, "Your NexChat verification code",
            $"Your NexChat verification code is {otp}. It expires in 5 minutes.", ct);
    }

    public Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken ct = default)
    {
        return SendAsync(email, "Welcome to NexChat",
            $"Welcome to NexChat, {displayName}. Your account is ready.", ct);
    }

    public Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken ct = default)
    {
        return SendAsync(email, "Reset your NexChat password",
            $"Use this link to reset your NexChat password: {resetLink}", ct);
    }

    public Task SendSecurityAlertEmailAsync(string email, string alertMessage, CancellationToken ct = default)
    {
        return SendAsync(email, "NexChat security alert", alertMessage, ct);
    }

    private async Task SendAsync(string to, string subject, string textBody, CancellationToken ct)
    {
        var host = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("SMTP host is not configured. Email to {Email} was skipped.", to);
            return;
        }

        var from = configuration["Smtp:From"] ?? "noreply@chatapp.local";
        var port = int.TryParse(configuration["Smtp:Port"], out var configuredPort) ? configuredPort : 25;
        var enableSsl = bool.TryParse(configuration["Smtp:EnableSsl"], out var configuredSsl) && configuredSsl;
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = textBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, password ?? string.Empty, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        logger.LogInformation("Email sent to {Email} with subject {Subject}", to, subject);
    }
}
