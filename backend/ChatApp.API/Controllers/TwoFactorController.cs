using System.IdentityModel.Tokens.Jwt;
using ChatApp.API.Extensions;
using ChatApp.Application.Commands.Auth;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/auth/2fa")]
public class TwoFactorController(
    IUnitOfWork uow,
    ITotpService totpService,
    IConfiguration config) : ControllerBase
{
    [HttpPost("setup")]
    public async Task<ActionResult<ApiResponse<TwoFactorSetupResponse>>> Setup(CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new KeyNotFoundException();
 
        if (user.TwoFactorEnabled)
            return BadRequest(ApiResponse<object>.Fail("2FA is already enabled."));
 
        var secret = totpService.GenerateSecret();
        var issuer = config["Jwt:Issuer"] ?? "ChatApp";
        var qrUri  = totpService.GenerateQrCodeUri(secret, user.Username, issuer);
 
        // Store pending secret — user must confirm before enabling
        user.TwoFactorSecret = secret;
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);
 
        return Ok(ApiResponse<TwoFactorSetupResponse>.Ok(
            new TwoFactorSetupResponse(secret, qrUri, [])));
    }
 
    [HttpPost("enable")]
    public async Task<ActionResult<ApiResponse<object>>> Enable(
        [FromBody] Enable2FaRequest request, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new KeyNotFoundException();
 
        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return BadRequest(ApiResponse<object>.Fail("Run /setup first."));
 
        if (!totpService.ValidateTotp(user.TwoFactorSecret, request.TotpCode))
            throw new UnauthorizedAccessException("Invalid TOTP code.");
 
        var backupCodes = totpService.GenerateBackupCodes();
        user.BackupCodes = backupCodes.Select(c => BCrypt.Net.BCrypt.HashPassword(c)).ToArray();
        user.TwoFactorEnabled = true;
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);
 
        return Ok(ApiResponse<object>.Ok(new { BackupCodes = backupCodes },
            "2FA enabled. Store your backup codes securely — they will not be shown again."));
    }
 
    [HttpPost("disable")]
    public async Task<ActionResult<ApiResponse<object>>> Disable(
        [FromBody] Verify2FaRequest request, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new KeyNotFoundException();
 
        if (!user.TwoFactorEnabled)
            return BadRequest(ApiResponse<object>.Fail("2FA is not enabled."));
 
        if (!totpService.ValidateTotp(user.TwoFactorSecret!, request.TotpCode))
            throw new UnauthorizedAccessException("Invalid TOTP code.");
 
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret  = null;
        user.BackupCodes      = null;
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);
 
        return Ok(ApiResponse<object>.Ok(new { }, "2FA disabled."));
    }
}