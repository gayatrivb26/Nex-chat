using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Username).HasMaxLength(30).IsRequired();
        b.Property(u => u.Email).HasMaxLength(255);
        b.Property(u => u.Phone).HasMaxLength(20).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(u => u.AvatarUrl).HasMaxLength(2000);
        b.Property(u => u.DisplayName).HasMaxLength(100);
        b.Property(u => u.Bio).HasMaxLength(500);
        b.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(u => u.TwoFactorSecret).HasMaxLength(255);
        b.Property(u => u.BackupCodes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");

        b.HasIndex(u => u.Username).IsUnique();
        b.HasIndex(u => u.Email).IsUnique().HasFilter("email IS NOT NULL");
        b.HasIndex(u => u.Phone).IsUnique();
        b.HasIndex(u => u.Status);

        b.Ignore(u => u.DomainEvents);
    }
}
