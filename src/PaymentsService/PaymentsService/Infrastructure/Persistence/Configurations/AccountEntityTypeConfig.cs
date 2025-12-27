using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsService.Domain.Entities;

namespace PaymentsService.Infrastructure.Persistence.Configurations;

public sealed class AccountEntityTypeConfig : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("accounts");

        b.HasKey(x => x.UserId);
        b.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(128).IsRequired();

        b.Property(x => x.BalanceMinor).HasColumnName("balance_minor").IsRequired();

        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
    }
}
