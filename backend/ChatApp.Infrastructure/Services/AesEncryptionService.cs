using System.Security.Cryptography;
using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ChatApp.Infrastructure.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var configured = configuration["Encryption:Key"];
        _key = !string.IsNullOrWhiteSpace(configured)
            ? SHA256.HashData(Convert.FromBase64String(configured))
            : SHA256.HashData("chatapp-development-encryption-key"u8.ToArray());
    }

    public string Encrypt(string plainText) => Convert.ToBase64String(EncryptBytes(System.Text.Encoding.UTF8.GetBytes(plainText)));

    public string Decrypt(string cipherText) => System.Text.Encoding.UTF8.GetString(DecryptBytes(Convert.FromBase64String(cipherText)));

    public byte[] EncryptBytes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
        return aes.IV.Concat(cipher).ToArray();
    }

    public byte[] DecryptBytes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = data[..16];
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 16, data.Length - 16);
    }
}
