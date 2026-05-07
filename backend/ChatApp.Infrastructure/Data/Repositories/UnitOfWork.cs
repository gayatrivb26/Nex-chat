using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
namespace ChatApp.Infrastructure.Data.Repositories;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private IUserRepository? _users;
    private IConversationRepository? _conversations;
    private IMessageRepository? _messages;
    private IRefreshTokenRepository? _refreshTokens;
    private IMediaFileRepository? _mediaFiles;
    private ICallLogRepository? _callLogs;
    private INotificationRepository? _notifications;
    private IKeyBundleRepository? _keyBundles;
    private IUserContactRepository? _userContacts;

    public IUserRepository Users => _users ??= new UserRepository(db);
    public IConversationRepository Conversations => _conversations ??= new ConversationRepository(db);
    public IMessageRepository Messages => _messages ??= new MessageRepository(db);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(db);
    public IMediaFileRepository MediaFiles => _mediaFiles ??= new MediaFileRepository(db);
    public ICallLogRepository CallLogs => _callLogs ??= new CallLogRepository(db);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(db);
    public IKeyBundleRepository KeyBundles => _keyBundles ??= new KeyBundleRepository(db);
    public IUserContactRepository UserContacts => _userContacts ??= new UserContactRepository(db);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => await db.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
        => await db.Database.CommitTransactionAsync(ct);

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
        => await db.Database.RollbackTransactionAsync(ct);
}
