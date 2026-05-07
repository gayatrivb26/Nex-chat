using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> b)
    {
        b.ToTable("MediaFiles");
        b.HasKey(f => f.Id);
        b.Property(f => f.OriginalName).HasMaxLength(255);
        b.Property(f => f.StoredName).HasMaxLength(255).IsRequired();
        b.Property(f => f.BucketName).HasMaxLength(100).IsRequired();
        b.Property(f => f.MimeType).HasMaxLength(100);
        b.Property(f => f.Checksum).HasMaxLength(64);
        b.Property(f => f.ScanResult).HasMaxLength(50);

        b.HasOne(f => f.UploadedBy).WithMany()
            .HasForeignKey(f => f.UploadedById).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(f => f.StoredName).IsUnique();
        b.Ignore(f => f.DomainEvents);
    }
}
