using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class OneTimePreKeyConfiguration : IEntityTypeConfiguration<OneTimePreKey>
{
    public void Configure(EntityTypeBuilder<OneTimePreKey> b)
    {
        b.ToTable("OneTimePreKeys");
        b.HasKey(k => k.Id);
        b.HasOne(k => k.User).WithMany()
            .HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(k => new { k.UserId, k.KeyId }).IsUnique();
        b.HasIndex(k => new { k.UserId, k.IsUsed });
        b.Ignore(k => k.DomainEvents);
    }
}
