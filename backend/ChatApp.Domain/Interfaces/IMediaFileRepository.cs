using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface IMediaFileRepository : IRepository<MediaFile>
{
    Task<MediaFile?> GetByStoredNameAsync(string storedName, CancellationToken ct = default);
    Task<IEnumerable<MediaFile>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IEnumerable<MediaFile>> GetPendingScanAsync(int batchSize = 50, CancellationToken ct = default);
}
