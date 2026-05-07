using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Db = db;
    protected readonly DbSet<T> Set = db.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Set.FindAsync([id], ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await Set.ToListAsync(ct);

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await Set.Where(predicate).ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await Set.AddRangeAsync(entities, ct);

    public virtual void Update(T entity)
    {
        Set.Attach(entity);
        Db.Entry(entity).State = EntityState.Modified;
    }

    public virtual void Remove(T entity) => Set.Remove(entity);

    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await Db.SaveChangesAsync(ct);
}
