using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdersService.Domain.Entities;

namespace OrdersService.Infrastructure.Persistence.Configurations;

public sealed class OrderEntityTypeConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");

        b.HasKey(x => x.OrderId);
        b.Property(x => x.OrderId).HasColumnName("order_id");

        b.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(128).IsRequired();
        b.Property(x => x.AmountMinor).HasColumnName("amount_minor").IsRequired();

        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);

        b.Property(x => x.Status).HasColumnName("status").IsRequired();

        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc }).HasDatabaseName("ix_orders_user_created");
    }
}
