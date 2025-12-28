using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsService.Infrastructure.Outbox;

namespace PaymentsService.Infrastructure.Persistence.Configurations;

public sealed class OutboxEntityTypeConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");

        b.HasKey(x => x.OutboxId);
        b.Property(x => x.OutboxId).HasColumnName("outbox_id");

        b.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        b.HasIndex(x => x.MessageId).IsUnique().HasDatabaseName("ux_outbox_message_id");

        b.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(256).IsRequired();
        b.Property(x => x.Payload).HasColumnName("payload").IsRequired();

        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        b.Property(x => x.SentAtUtc).HasColumnName("sent_at_utc");

        b.Property(x => x.LockedBy).HasColumnName("locked_by").HasMaxLength(128);
        b.Property(x => x.LockedUntilUtc).HasColumnName("locked_until_utc");

        b.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
        b.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
        b.Property(x => x.LastError).HasColumnName("last_error");

        b.HasIndex(x => new { x.SentAtUtc, x.NextAttemptAtUtc, x.LockedUntilUtc })
            .HasDatabaseName("ix_outbox_pick");
    }
}
