using System.Security.Cryptography;
using ChatApp.Domain.Interfaces;
using OtpNet;

namespace ChatApp.Infrastructure.Security;

public class TotpService : ITotpService
{
    public string GenerateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public string GenerateQrCodeUri(string secret, string username, string issuer)
        => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(username)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

    public bool ValidateTotp(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    public string[] GenerateBackupCodes(int count = 8)
        => Enumerable.Range(0, count)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant())
            .ToArray();

    public bool ValidateBackupCode(string[] storedHashes, string providedCode, out string[]? updatedHashes)
    {
        var list = storedHashes.ToList();
        var match = list.FirstOrDefault(hash => BCrypt.Net.BCrypt.Verify(providedCode, hash));
        if (match == null)
        {
            updatedHashes = null;
            return false;
        }

        list.Remove(match);
        updatedHashes = list.ToArray();
        return true;
    }
}
