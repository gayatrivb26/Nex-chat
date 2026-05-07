using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
namespace ChatApp.Application.Commands.Auth;

public class Enable2FaHandler(IUnitOfWork uow, ITotpService totpSvc)
    : IRequestHandler<Enable2FaCommand, string[]>
{
    public async Task<string[]> Handle(Enable2FaCommand cmd, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(cmd.UserId, ct)
            ?? throw new InvalidOperationException("User not found.");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new InvalidOperationException("2FA setup not initiated.");

        if (!totpSvc.ValidateTotp(user.TwoFactorSecret, cmd.TotpCode))
            throw new UnauthorizedAccessException("Invalid TOTP code.");

        var backupCodes = totpSvc.GenerateBackupCodes();
        var hashedCodes = backupCodes.Select(c => BCrypt.Net.BCrypt.HashPassword(c)).ToArray();

        user.TwoFactorEnabled = true;
        user.BackupCodes = hashedCodes;
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        return backupCodes; // Return plain codes once - user must save them
    }
}
