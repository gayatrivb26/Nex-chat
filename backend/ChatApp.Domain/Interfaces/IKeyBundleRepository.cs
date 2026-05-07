using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface IKeyBundleRepository : IRepository<KeyBundle>
{
    Task<KeyBundle?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<OneTimePreKey?> ClaimOneTimePreKeyAsync(Guid userId, CancellationToken ct = default);
    Task AddOneTimePreKeysAsync(Guid userId, IEnumerable<(int KeyId, string PublicKey)> keys, CancellationToken ct = default);
    Task<int> GetOneTimePreKeyCountAsync(Guid userId, CancellationToken ct = default);
}
