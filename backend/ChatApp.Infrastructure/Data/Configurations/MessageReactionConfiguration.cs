using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> b)
    {
        b.ToTable("MessageReactions");
        b.HasKey(r => r.Id);
        b.Property(r => r.Emoji).HasMaxLength(10).IsRequired();

        b.HasOne(r => r.Message).WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(r => new { r.MessageId, r.UserId, r.Emoji }).IsUnique();
        b.Ignore(r => r.DomainEvents);
    }
}
