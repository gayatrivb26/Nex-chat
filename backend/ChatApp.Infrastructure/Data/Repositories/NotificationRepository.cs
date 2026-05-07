using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class NotificationRepository(AppDbContext db) : Repository<Notification>(db), INotificationRepository
{
    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(
        Guid userId, int skip = 0, int take = 20, CancellationToken ct = default)
        => await Set.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await Set.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
        => await Set.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
}
