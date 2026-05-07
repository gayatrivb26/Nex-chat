using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Data.Configurations;

public class UserContactConfiguration : IEntityTypeConfiguration<UserContact>
{
    public void Configure(EntityTypeBuilder<UserContact> b)
    {
        b.ToTable("UserContacts");
        b.HasKey(c => c.Id);
        b.Property(c => c.Nickname).HasMaxLength(100);

        b.HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.ContactUser).WithMany()
            .HasForeignKey(c => c.ContactUserId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(c => new { c.UserId, c.ContactUserId }).IsUnique();
        b.Ignore(c => c.DomainEvents);
    }
}
