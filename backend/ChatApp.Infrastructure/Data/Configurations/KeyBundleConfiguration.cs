using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class KeyBundleConfiguration : IEntityTypeConfiguration<KeyBundle>
{
    public void Configure(EntityTypeBuilder<KeyBundle> b)
    {
        b.ToTable("KeyBundles");
        b.HasKey(k => k.Id);
        b.HasOne(k => k.User).WithMany()
            .HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(k => k.UserId).IsUnique();
        b.Ignore(k => k.DomainEvents);
    }
}
