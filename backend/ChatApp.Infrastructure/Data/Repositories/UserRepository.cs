using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class UserRepository(AppDbContext db) : Repository<User>(db), IUserRepository
{
    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(u => u.Phone == phone, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task<bool> PhoneExistsAsync(string phone, CancellationToken ct = default)
        => await Set.AnyAsync(u => u.Phone == phone, ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await Set.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
        => await Set.AnyAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task<IEnumerable<User>> SearchUsersAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var q = query.ToLowerInvariant();
        return await Set
            .Where(u => u.Username.Contains(q) || (u.DisplayName != null && u.DisplayName.ToLower().Contains(q)) || u.Phone.Contains(q))
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<User>> GetOnlineUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
        => await Set.Where(u => userIds.Contains(u.Id) && u.Status == UserStatus.Online).ToListAsync(ct);

    public async Task<User?> GetWithKeyBundleAsync(Guid userId, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(u => u.Id == userId, ct);
}
