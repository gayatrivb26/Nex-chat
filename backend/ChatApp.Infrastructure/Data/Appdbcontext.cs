using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageStatus> MessageStatuses => Set<MessageStatus>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<UserContact> UserContacts => Set<UserContact>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();
    public DbSet<KeyBundle> KeyBundles => Set<KeyBundle>();
    public DbSet<OneTimePreKey> OneTimePreKeys => Set<OneTimePreKey>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        mb.HasDefaultSchema("public");

        // ── Global query filters ──────────────────────────────────────
        // Only filter soft-deleted Users — consumers check IsDeleted explicitly for other needs
        mb.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        // Conversations: hide hard-deleted ones globally
        mb.Entity<Conversation>().HasQueryFilter(c => !c.IsDeleted);

        // Messages: hide "deleted for everyone". Sender-deleted messages are
        // still visible to sender — handled in service/query layer, NOT here.
        // A global filter on IsDeleted would break sender's "deleted" view.
        // We intentionally DO NOT filter messages here.

        // ConversationMembers: DO NOT filter by LeftAt globally.
        // Left members still need to be fetched for: history access, admin checks,
        // and private conversation lookup. Apply LeftAt filter per-query instead.
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors(false);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Future: dispatch domain events via IPublisher here if needed
        return base.SaveChangesAsync(ct);
    }
}