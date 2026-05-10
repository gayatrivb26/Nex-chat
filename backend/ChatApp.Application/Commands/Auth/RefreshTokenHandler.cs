using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public class RefreshTokenHandler(
    IUnitOfWork uow,
    ITokenService tokenSvc,
    ICacheService cacheSvc,
    IAuditService auditSvc,
    ILogger<RefreshTokenHandler> logger) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        // Reference cacheSvc to avoid CS9113 if not used yet
        _ = cacheSvc;
        var familyId = tokenSvc.TryGetRefreshTokenFamilyId(cmd.RawToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        var familyTokens = await uow.RefreshTokens.GetFamilyAsync(familyId, ct);
        var storedToken = familyTokens.FirstOrDefault(t => tokenSvc.VerifyRefreshToken(cmd.RawToken, t.TokenHash))
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!storedToken.IsValid())
        {
            // Detect reuse: revoke entire family
            if (storedToken.RevokedAt != null)
            {
                await uow.RefreshTokens.RevokeFamilyAsync(storedToken.FamilyId, ct);
                await uow.SaveChangesAsync(ct);
                await auditSvc.LogAsync("TokenReuseDetected", storedToken.UserId, ip: cmd.IpAddress, ct: ct);
                logger.LogWarning("Refresh token reuse detected for user {UserId}", storedToken.UserId);
                throw new UnauthorizedAccessException("Token reuse detected. All sessions revoked.");
            }
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        var user = await uow.Users.GetByIdAsync(storedToken.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        // Rotate: revoke old, issue new in same family
        var (rawNew, hashNew, _) = tokenSvc.GenerateRefreshToken(familyId);
        var newToken = RefreshToken.Create(user.Id, hashNew, storedToken.FamilyId, 30,
            storedToken.DeviceName, storedToken.DeviceType, cmd.IpAddress, storedToken.UserAgent);

        storedToken.Revoke(newToken.Id);
        uow.RefreshTokens.Update(storedToken);
        await uow.RefreshTokens.AddAsync(newToken, ct);

        var (accessToken, jti, expiry) = tokenSvc.GenerateAccessToken(user);
        await uow.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, rawNew, expiry, MapUser(user));
    }

    private static UserDto MapUser(User u) => new(u.Id, u.Username, u.Email, u.Phone,
        u.AvatarUrl, u.DisplayName, u.Bio, u.Status.ToString(),
        u.LastSeen, u.IsVerified, u.TwoFactorEnabled, u.CreatedAt);
}
