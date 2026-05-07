using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(t => t.Id);
        b.Property(t => t.TokenHash).HasMaxLength(255).IsRequired();
        b.Property(t => t.DeviceName).HasMaxLength(255);
        b.Property(t => t.DeviceType).HasMaxLength(50);
        b.Property(t => t.IpAddress).HasMaxLength(45);

        b.HasOne(t => t.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasIndex(t => t.FamilyId).HasDatabaseName("idx_refreshtokens_family");
        b.HasIndex(t => t.UserId);
        b.Ignore(t => t.DomainEvents);
    }
}
