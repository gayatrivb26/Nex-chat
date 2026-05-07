using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces;

public interface IUserContactRepository : IRepository<UserContact>
{
    Task<UserContact?> GetContactAsync(Guid userId, Guid contactUserId, CancellationToken ct = default);
    Task<IEnumerable<UserContact>> GetUserContactsAsync(Guid userId, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<IEnumerable<UserContact>> GetBlockedContactsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsBlockedAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
}
