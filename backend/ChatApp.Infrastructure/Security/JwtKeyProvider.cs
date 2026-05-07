using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Infrastructure.Security;

public class JwtKeyProvider
{
    public RsaSecurityKey SecurityKey { get; }
    public SigningCredentials SigningCredentials { get; }

    public JwtKeyProvider(IConfiguration configuration)
    {
        var rsa = RSA.Create();
        var privateKeyPath = configuration["Jwt:PrivateKeyPath"];
        var privateKey = !string.IsNullOrWhiteSpace(privateKeyPath) && File.Exists(privateKeyPath)
            ? File.ReadAllText(privateKeyPath)
            : configuration["Jwt:PrivateKey"];

        if (!string.IsNullOrWhiteSpace(privateKey))
            rsa.ImportFromPem(privateKey);
        else
            rsa.KeySize = 2048;

        SecurityKey = new RsaSecurityKey(rsa) { KeyId = configuration["Jwt:KeyId"] ?? "chatapp-dev-key" };
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.RsaSha256);
    }

    public TokenValidationParameters CreateValidationParameters(string issuer, string audience)
        => new()
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
}
