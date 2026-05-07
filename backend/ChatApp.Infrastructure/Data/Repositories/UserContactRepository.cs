using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Data.Repositories;

public class UserContactRepository(AppDbContext db) : Repository<UserContact>(db), IUserContactRepository
{
    public async Task<UserContact?> GetContactAsync(Guid userId, Guid contactUserId, CancellationToken ct = default)
        => await Set.Include(c => c.ContactUser)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId, ct);

    public async Task<IEnumerable<UserContact>> GetUserContactsAsync(Guid userId, int skip = 0, int take = 100, CancellationToken ct = default)
        => await Set.Include(c => c.ContactUser)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.ContactUser!.DisplayName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IEnumerable<UserContact>> GetBlockedContactsAsync(Guid userId, CancellationToken ct = default)
        => await Set.Include(c => c.ContactUser)
            .Where(c => c.UserId == userId && c.IsBlocked)
            .ToListAsync(ct);

    public async Task<bool> IsBlockedAsync(Guid userId, Guid otherUserId, CancellationToken ct = default)
        => await Set.AnyAsync(c =>
            ((c.UserId == userId && c.ContactUserId == otherUserId) ||
             (c.UserId == otherUserId && c.ContactUserId == userId)) &&
            c.IsBlocked, ct);
}
