using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(a => a.Id);
        b.Property(a => a.Action).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityType).HasMaxLength(100);
        b.Property(a => a.IpAddress).HasMaxLength(45);
        b.Property(a => a.OldValues).HasColumnType("jsonb");
        b.Property(a => a.NewValues).HasColumnType("jsonb");

        b.HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(a => new { a.UserId, a.CreatedAt });
        b.Ignore(a => a.DomainEvents);
    }
}
