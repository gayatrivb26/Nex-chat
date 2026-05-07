using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class MessageStatusConfiguration : IEntityTypeConfiguration<MessageStatus>
{
    public void Configure(EntityTypeBuilder<MessageStatus> b)
    {
        b.ToTable("MessageStatuses");
        b.HasKey(s => s.Id);
        b.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        b.HasOne(s => s.Message).WithMany(m => m.Statuses)
            .HasForeignKey(s => s.MessageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(s => s.User).WithMany()
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(s => new { s.MessageId, s.UserId }).IsUnique();
        b.HasIndex(s => new { s.UserId, s.Status }).HasDatabaseName("idx_messagestatus_user");
        b.Ignore(s => s.DomainEvents);
    }
}
