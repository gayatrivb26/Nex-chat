using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IRateLimitService
{
    Task<(bool allowed, int remaining, TimeSpan retryAfter)> CheckRateLimitAsync(
        string key, int limit, TimeSpan window, CancellationToken ct = default);
}
