using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public class LoginHandler(
    IUnitOfWork uow,
    IPasswordService passwordSvc,
    ITokenService tokenSvc,
    ITotpService totpSvc,
    IAuditService auditSvc,
    ILogger<LoginHandler> logger) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await uow.Users.GetByPhoneAsync(cmd.Phone, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.IsDeleted) throw new UnauthorizedAccessException("Account not found.");
        if (user.IsLocked()) throw new UnauthorizedAccessException(
            $"Account locked. Try again after {user.LockedUntil:HH:mm} UTC.");

        if (!passwordSvc.Verify(cmd.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            uow.Users.Update(user);
            await uow.SaveChangesAsync(ct);
            await auditSvc.LogAsync("FailedLogin", user.Id, ip: cmd.IpAddress, ct: ct);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        // 2FA check
        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(cmd.TotpCode))
                throw new InvalidOperationException("2FA_REQUIRED");

            var isBackupCode = cmd.TotpCode.Length == 8 && !cmd.TotpCode.All(char.IsDigit);
            if (isBackupCode)
            {
                if (!totpSvc.ValidateBackupCode(user.BackupCodes ?? [], cmd.TotpCode, out var updated))
                    throw new UnauthorizedAccessException("Invalid backup code.");
                user.BackupCodes = updated;
            }
            else if (!totpSvc.ValidateTotp(user.TwoFactorSecret!, cmd.TotpCode))
                throw new UnauthorizedAccessException("Invalid 2FA code.");
        }

        user.ResetFailedLogins();
        user.SetOnline();
        uow.Users.Update(user);

        var (accessToken, jti, expiry) = tokenSvc.GenerateAccessToken(user);
        var (rawToken, tokenHash, familyId) = tokenSvc.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(user.Id, tokenHash, familyId, 30,
            cmd.DeviceName, cmd.DeviceType, cmd.IpAddress, cmd.UserAgent);
        await uow.RefreshTokens.AddAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        await auditSvc.LogAsync("UserLoggedIn", user.Id, ip: cmd.IpAddress, ct: ct);
        logger.LogInformation("User {UserId} logged in from {IP}", user.Id, cmd.IpAddress);

        return new AuthResponse(accessToken, rawToken, expiry, MapUser(user));
    }

    private static UserDto MapUser(User u) => new(u.Id, u.Username, u.Email, u.Phone,
        u.AvatarUrl, u.DisplayName, u.Bio, u.Status.ToString(),
        u.LastSeen, u.IsVerified, u.TwoFactorEnabled, u.CreatedAt);
}
