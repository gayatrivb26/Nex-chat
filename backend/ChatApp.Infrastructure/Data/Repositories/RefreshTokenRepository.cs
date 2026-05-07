using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class RefreshTokenRepository(AppDbContext db) : Repository<RefreshToken>(db), IRefreshTokenRepository
{
    private static string ComputeHmacSha256(string value)
    {
        // Use HMAC-SHA256 for fast lookup (not bcrypt)
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("nexchat-rt-secret"));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => await Set.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IEnumerable<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken ct = default)
        => await Set.Where(t => t.FamilyId == familyId).ToListAsync(ct);

    public async Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
        => await Set.Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(Guid userId, CancellationToken ct = default)
        => await Set.Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default)
        => await Set.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);

    public async Task PurgeExpiredAsync(CancellationToken ct = default)
        => await Set.Where(t => t.ExpiresAt < DateTime.UtcNow.AddDays(-7))
            .ExecuteDeleteAsync(ct);
}
