using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface ITotpService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string secret, string username, string issuer);
    bool ValidateTotp(string secret, string code);
    string[] GenerateBackupCodes(int count = 8);
    bool ValidateBackupCode(string[] storedHashes, string providedCode, out string[]? updatedHashes);
}
