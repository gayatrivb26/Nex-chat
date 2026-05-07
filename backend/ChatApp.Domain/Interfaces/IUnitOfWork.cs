using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using System.Linq.Expressions;
namespace ChatApp.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IConversationRepository Conversations { get; }
    IMessageRepository Messages { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IMediaFileRepository MediaFiles { get; }
    ICallLogRepository CallLogs { get; }
    INotificationRepository Notifications { get; }
    IKeyBundleRepository KeyBundles { get; }
    IUserContactRepository UserContacts { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
