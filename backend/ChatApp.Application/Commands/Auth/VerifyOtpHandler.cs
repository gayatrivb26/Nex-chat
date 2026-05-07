using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public class VerifyOtpHandler(
    IUnitOfWork uow,
    IOtpService otpSvc,
    ITokenService tokenSvc,
    IAuditService auditSvc,
    ILogger<VerifyOtpHandler> logger) : IRequestHandler<VerifyOtpCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(VerifyOtpCommand cmd, CancellationToken ct)
    {
        var user = await uow.Users.GetByPhoneAsync(cmd.Phone, ct)
            ?? throw new InvalidOperationException("User not found.");

        var valid = await otpSvc.ValidateOtpAsync(cmd.Phone, cmd.Otp, ct);
        if (!valid) throw new UnauthorizedAccessException("Invalid or expired OTP.");

        user.MarkVerified();
        uow.Users.Update(user);

        var (accessToken, jti, expiry) = tokenSvc.GenerateAccessToken(user);
        var (rawToken, tokenHash, familyId) = tokenSvc.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(user.Id, tokenHash, familyId, 30,
            cmd.DeviceName, cmd.DeviceType, cmd.IpAddress, cmd.UserAgent);
        await uow.RefreshTokens.AddAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        await auditSvc.LogAsync("UserVerified", user.Id, "User", user.Id, ip: cmd.IpAddress, ct: ct);
        logger.LogInformation("User {UserId} verified and logged in", user.Id);

        return new AuthResponse(accessToken, rawToken, expiry, MapUser(user));
    }

    private static UserDto MapUser(User u) => new(u.Id, u.Username, u.Email, u.Phone,
        u.AvatarUrl, u.DisplayName, u.Bio, u.Status.ToString(),
        u.LastSeen, u.IsVerified, u.TwoFactorEnabled, u.CreatedAt);
}
