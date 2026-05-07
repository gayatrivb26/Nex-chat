using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Data.Configurations;

public class CallLogConfiguration : IEntityTypeConfiguration<CallLog>
{
    public void Configure(EntityTypeBuilder<CallLog> b)
    {
        b.ToTable("CallLogs");
        b.HasKey(c => c.Id);
        b.Property(c => c.CallType).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.EndReason).HasMaxLength(50);
        b.Property(c => c.RecordingUrl).HasMaxLength(2000);

        b.HasOne(c => c.Conversation).WithMany()
            .HasForeignKey(c => c.ConversationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(c => c.Initiator).WithMany()
            .HasForeignKey(c => c.InitiatorId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(c => new { c.ConversationId, c.StartedAt }).IsDescending(false, true);
        b.Ignore(c => c.DomainEvents);
    }
}
