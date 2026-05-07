using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> PhoneExistsAsync(string phone, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    Task<IEnumerable<User>> SearchUsersAsync(string query, int limit = 20, CancellationToken ct = default);
    Task<IEnumerable<User>> GetOnlineUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task<User?> GetWithKeyBundleAsync(Guid userId, CancellationToken ct = default);
}
