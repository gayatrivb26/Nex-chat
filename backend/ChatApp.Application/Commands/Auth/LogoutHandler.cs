using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public class LogoutHandler(IUnitOfWork uow, ICacheService cacheSvc, IAuditService auditSvc, ITokenService tokenSvc)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        await cacheSvc.BlacklistJtiAsync(cmd.Jti, cmd.JtiTtl, ct);

        if (!string.IsNullOrEmpty(cmd.RefreshToken))
        {
            var familyId = tokenSvc.TryGetRefreshTokenFamilyId(cmd.RefreshToken);
            if (familyId.HasValue)
            {
                var familyTokens = await uow.RefreshTokens.GetFamilyAsync(familyId.Value, ct);
                var token = familyTokens.FirstOrDefault(t => tokenSvc.VerifyRefreshToken(cmd.RefreshToken, t.TokenHash));
                if (token != null)
                {
                    token.Revoke();
                    uow.RefreshTokens.Update(token);
                }
            }
        }

        await uow.SaveChangesAsync(ct);
        await auditSvc.LogAsync("UserLoggedOut", cmd.UserId, ct: ct);
    }
}
