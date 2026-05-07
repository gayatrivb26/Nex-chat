using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(n => n.Id);
        b.Property(n => n.Type).HasMaxLength(50).IsRequired();
        b.Property(n => n.Title).HasMaxLength(255);
        b.Property(n => n.Payload)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");

        b.HasOne(n => n.User).WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_notifications_user_unread");
        b.Ignore(n => n.DomainEvents);
    }
}
