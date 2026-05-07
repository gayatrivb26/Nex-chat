using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class MediaFileRepository(AppDbContext db) : Repository<MediaFile>(db), IMediaFileRepository
{
    public async Task<MediaFile?> GetByStoredNameAsync(string storedName, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(f => f.StoredName == storedName, ct);

    public async Task<IEnumerable<MediaFile>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
        => await Set.Where(f => f.UploadedById == userId)
            .OrderByDescending(f => f.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<IEnumerable<MediaFile>> GetPendingScanAsync(int batchSize = 50, CancellationToken ct = default)
        => await Set.Where(f => !f.IsScanned).Take(batchSize).ToListAsync(ct);
}
