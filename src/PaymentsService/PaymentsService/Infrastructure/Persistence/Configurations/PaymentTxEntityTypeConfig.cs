using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsService.Domain.Entities;

namespace PaymentsService.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionEntityTypeConfig : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> b)
    {
        b.ToTable("payment_transactions");

        b.HasKey(x => x.PaymentTransactionId);
        b.Property(x => x.PaymentTransactionId).HasColumnName("payment_tx_id");

        b.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        b.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("ux_payment_tx_order_id"); // защита от дублей

        b.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(128).IsRequired();
        b.Property(x => x.AmountMinor).HasColumnName("amount_minor").IsRequired();

        b.Property(x => x.Status).HasColumnName("status").IsRequired();
        b.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(128);

        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc }).HasDatabaseName("ix_payment_tx_user_created");
    }
}
