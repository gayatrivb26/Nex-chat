using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

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

        // Global query filters
        mb.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        mb.Entity<Conversation>().HasQueryFilter(c => !c.IsDeleted);
        mb.Entity<Message>().HasQueryFilter(m => !m.IsDeleted || m.DeleteForEveryone == false);
        mb.Entity<ConversationMember>().HasQueryFilter(m => m.LeftAt == null);
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
        // Dispatch domain events before saving
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        // Note: domain event dispatching is handled by the DomainEventDispatcher service
        // called from the application layer after SaveChanges

        return base.SaveChangesAsync(ct);
    }
}