using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("Conversations");
        b.HasKey(c => c.Id);
        b.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.Name).HasMaxLength(100);
        b.Property(c => c.Description).HasMaxLength(500);
        b.Property(c => c.AvatarUrl).HasMaxLength(2000);

        b.HasOne(c => c.CreatedBy).WithMany()
            .HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.SetNull);

        b.HasOne(c => c.LastMessage).WithMany()
            .HasForeignKey(c => c.LastMessageId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(c => c.LastActivityAt)
            .IsDescending()
            .HasDatabaseName("idx_conversations_activity");
        b.Ignore(c => c.DomainEvents);
    }
}
