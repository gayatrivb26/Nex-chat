using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class KeyBundleRepository(AppDbContext db) : Repository<KeyBundle>(db), IKeyBundleRepository
{
    public async Task<KeyBundle?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(k => k.UserId == userId, ct);

    public async Task<OneTimePreKey?> ClaimOneTimePreKeyAsync(Guid userId, CancellationToken ct = default)
    {
        var key = await Db.OneTimePreKeys
            .Where(k => k.UserId == userId && !k.IsUsed)
            .OrderBy(k => k.KeyId)
            .FirstOrDefaultAsync(ct);

        if (key != null)
        {
            key.MarkUsed();
            await Db.SaveChangesAsync(ct);
        }
        return key;
    }

    public async Task AddOneTimePreKeysAsync(Guid userId,
        IEnumerable<(int KeyId, string PublicKey)> keys, CancellationToken ct = default)
    {
        var entities = keys.Select(k => OneTimePreKey.Create(userId, k.KeyId, k.PublicKey));
        await Db.OneTimePreKeys.AddRangeAsync(entities, ct);
        await Db.SaveChangesAsync(ct);
    }

    public async Task<int> GetOneTimePreKeyCountAsync(Guid userId, CancellationToken ct = default)
        => await Db.OneTimePreKeys.CountAsync(k => k.UserId == userId && !k.IsUsed, ct);
}
