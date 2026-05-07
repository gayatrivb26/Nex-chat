using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> b)
    {
        b.ToTable("ConversationMembers");
        b.HasKey(m => m.Id);
        b.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        b.HasOne(m => m.Conversation).WithMany(c => c.Members)
            .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(m => m.User).WithMany(u => u.ConversationMemberships)
            .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(m => new { m.ConversationId, m.UserId }).IsUnique();
        b.HasIndex(m => m.UserId);
        b.Ignore(m => m.DomainEvents);
    }
}
