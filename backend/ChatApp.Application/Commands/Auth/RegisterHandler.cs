using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public class RegisterHandler(
    IUnitOfWork uow,
    IPasswordService passwordSvc,
    IOtpService otpSvc,
    IEmailService emailSvc,
    ILogger<RegisterHandler> logger) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await uow.Users.PhoneExistsAsync(cmd.Phone, ct))
            throw new InvalidOperationException("Phone number already registered.");
        if (await uow.Users.UsernameExistsAsync(cmd.Username, ct))
            throw new InvalidOperationException("Username already taken.");
        if (cmd.Email != null && await uow.Users.EmailExistsAsync(cmd.Email, ct))
            throw new InvalidOperationException("Email already registered.");

        var hash = passwordSvc.Hash(cmd.Password);
        var user = User.Create(cmd.Username, cmd.Phone, hash, cmd.Email);

        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        // Send OTP for verification
        var sendAllowed = await otpSvc.TryMarkOtpSendAsync(cmd.Phone, 3, TimeSpan.FromMinutes(10), ct);
        if (!sendAllowed)
            throw new InvalidOperationException("Too many OTP requests. Please try again later.");

        var otp = await otpSvc.GenerateAndStoreOtpAsync(cmd.Phone, ct);
        logger.LogInformation("OTP generated for new user {UserId}", user.Id);

        // Send via email as fallback if email provided
        if (cmd.Email != null)
            await emailSvc.SendOtpEmailAsync(cmd.Email, otp, ct);

        // Return minimal response - user must verify OTP before getting tokens
        return new AuthResponse(string.Empty, string.Empty, DateTime.UtcNow, MapUser(user));
    }

    private static UserDto MapUser(User u) => new(u.Id, u.Username, u.Email, u.Phone,
        u.AvatarUrl, u.DisplayName, u.Bio, u.Status.ToString(),
        u.LastSeen, u.IsVerified, u.TwoFactorEnabled, u.CreatedAt);
}
