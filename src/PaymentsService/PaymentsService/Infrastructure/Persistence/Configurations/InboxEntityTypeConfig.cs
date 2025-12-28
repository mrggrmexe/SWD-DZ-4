using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsService.Infrastructure.Outbox;

namespace PaymentsService.Infrastructure.Persistence.Configurations;

public sealed class InboxEntityTypeConfig : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> b)
    {
        b.ToTable("inbox_messages");

        b.HasKey(x => x.InboxId);
        b.Property(x => x.InboxId).HasColumnName("inbox_id");

        b.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        b.HasIndex(x => x.MessageId).IsUnique().HasDatabaseName("ux_inbox_message_id");

        b.Property(x => x.Consumer).HasColumnName("consumer").HasMaxLength(128).IsRequired();
        b.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc").IsRequired();
    }
}
