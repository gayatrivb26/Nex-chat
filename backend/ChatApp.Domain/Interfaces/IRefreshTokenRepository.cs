using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IEnumerable<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken ct = default);
    Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(Guid userId, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default);
    Task PurgeExpiredAsync(CancellationToken ct = default);
}
