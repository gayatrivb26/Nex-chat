using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Infrastructure.Security;

public class TokenService : ITokenService
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TokenValidationParameters _validationParameters;
    private readonly JwtKeyProvider _keyProvider;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;

    public TokenService(IConfiguration configuration, JwtKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
        _issuer = configuration["Jwt:Issuer"] ?? "ChatApp";
        _audience = configuration["Jwt:Audience"] ?? "ChatApp.Client";
        _accessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenExpiryMinutes"], out var minutes) ? minutes : 15;
        _validationParameters = keyProvider.CreateValidationParameters(_issuer, _audience);
    }

    public (string token, string jti, DateTime expiry) GenerateAccessToken(User user)
    {
        var jti = Guid.NewGuid().ToString("N");
        var expiry = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("phone", user.Phone),
            new Claim("is_verified", user.IsVerified ? "true" : "false")
        };

        var token = new JwtSecurityToken(_issuer, _audience, claims,
            notBefore: DateTime.UtcNow, expires: expiry, signingCredentials: _keyProvider.SigningCredentials);

        return (_handler.WriteToken(token), jti, expiry);
    }

    public (string rawToken, string tokenHash, Guid familyId) GenerateRefreshToken(Guid? familyId = null)
    {
        var actualFamilyId = familyId ?? Guid.NewGuid();
        var secretBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = $"{actualFamilyId:N}.{Base64UrlEncoder.Encode(secretBytes)}";
        return (rawToken, HashRefreshToken(rawToken), actualFamilyId);
    }

    public Guid? TryGetRefreshTokenFamilyId(string rawToken)
    {
        var dot = rawToken.IndexOf('.');
        if (dot <= 0) return null;
        return Guid.TryParseExact(rawToken[..dot], "N", out var familyId) ? familyId : null;
    }

    public string HashRefreshToken(string rawToken) => BCrypt.Net.BCrypt.HashPassword(rawToken, 12);

    public bool VerifyRefreshToken(string rawToken, string tokenHash) => BCrypt.Net.BCrypt.Verify(rawToken, tokenHash);

    public bool ValidateAccessToken(string token, out Guid userId, out string jti)
    {
        userId = Guid.Empty;
        jti = string.Empty;
        try
        {
            var principal = _handler.ValidateToken(token, _validationParameters, out _);
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;
            return Guid.TryParse(sub, out userId) && !string.IsNullOrWhiteSpace(jti);
        }
        catch
        {
            return false;
        }
    }
}
