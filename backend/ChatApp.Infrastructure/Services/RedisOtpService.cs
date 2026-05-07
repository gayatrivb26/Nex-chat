using System.Security.Cryptography;
using ChatApp.Domain.Interfaces;

namespace ChatApp.Infrastructure.Services;

public class RedisOtpService(ICacheService cache) : IOtpService
{
    public async Task<bool> TryMarkOtpSendAsync(string phone, int maxSends, TimeSpan window, CancellationToken ct = default)
    {
        var key = $"otp:{phone}:send_count";
        if (!await cache.ExistsAsync(key, ct))
            await cache.SetAsync(key, 0, window, ct);

        var count = await cache.IncrementAsync(key, ct);
        return count <= maxSends;
    }

    public async Task<string> GenerateAndStoreOtpAsync(string phone, CancellationToken ct = default)
    {
        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        await cache.SetAsync($"otp:{phone}", BCrypt.Net.BCrypt.HashPassword(otp, 12), TimeSpan.FromMinutes(5), ct);
        await cache.SetAsync($"otp:{phone}:attempts", 0, TimeSpan.FromMinutes(5), ct);
        return otp;
    }

    public async Task<bool> ValidateOtpAsync(string phone, string otp, CancellationToken ct = default)
    {
        var attempts = await cache.IncrementAsync($"otp:{phone}:attempts", ct);
        if (attempts > 5)
        {
            await InvalidateOtpAsync(phone, ct);
            return false;
        }

        var hash = await cache.GetAsync<string>($"otp:{phone}", ct);
        if (string.IsNullOrWhiteSpace(hash)) return false;

        var valid = BCrypt.Net.BCrypt.Verify(otp, hash);
        if (valid) await InvalidateOtpAsync(phone, ct);
        return valid;
    }

    public async Task InvalidateOtpAsync(string phone, CancellationToken ct = default)
    {
        await cache.RemoveAsync($"otp:{phone}", ct);
        await cache.RemoveAsync($"otp:{phone}:attempts", ct);
    }
}
