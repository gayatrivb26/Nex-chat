using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IOtpService
{
    Task<bool> TryMarkOtpSendAsync(string phone, int maxSends, TimeSpan window, CancellationToken ct = default);
    Task<string> GenerateAndStoreOtpAsync(string phone, CancellationToken ct = default);
    Task<bool> ValidateOtpAsync(string phone, string otp, CancellationToken ct = default);
    Task InvalidateOtpAsync(string phone, CancellationToken ct = default);
}
