using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface ITokenService
{
    (string token, string jti, DateTime expiry) GenerateAccessToken(User user);
    (string rawToken, string tokenHash, Guid familyId) GenerateRefreshToken(Guid? familyId = null);
    Guid? TryGetRefreshTokenFamilyId(string rawToken);
    string HashRefreshToken(string rawToken);
    bool VerifyRefreshToken(string rawToken, string tokenHash);
    bool ValidateAccessToken(string token, out Guid userId, out string jti);
}
