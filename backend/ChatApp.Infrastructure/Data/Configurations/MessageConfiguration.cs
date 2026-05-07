using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using System.Text.Json;
namespace ChatApp.Infrastructure.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("Messages");
        b.HasKey(m => m.Id);
        b.Property(m => m.Content).HasColumnType("text");
        b.Property(m => m.EncryptedContent).HasColumnType("bytea");
        b.Property(m => m.MessageType).HasConversion<string>().HasMaxLength(20);
        b.Property(m => m.MediaUrl).HasMaxLength(2000);
        b.Property(m => m.ThumbnailUrl).HasMaxLength(2000);
        b.Property(m => m.FileName).HasMaxLength(255);
        b.Property(m => m.MimeType).HasMaxLength(100);
        b.Property(m => m.Metadata)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
        b.Property<NpgsqlTsVector>("SearchVector").HasColumnType("tsvector");

        b.HasOne(m => m.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(m => m.Sender).WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(m => m.ReplyToMessage).WithMany()
            .HasForeignKey(m => m.ReplyToMessageId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_messages_conversation_created");
        b.HasIndex(m => m.SenderId).HasDatabaseName("idx_messages_sender");
        b.HasIndex("SearchVector").HasMethod("GIN").HasDatabaseName("idx_messages_search");
        b.Ignore(m => m.DomainEvents);
    }
}
