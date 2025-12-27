using Microsoft.EntityFrameworkCore;
using PaymentsService.Domain.Entities;
using PaymentsService.Infrastructure.Outbox;
using PaymentsService.Infrastructure.Persistence.Configurations;

namespace PaymentsService.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AccountEntityTypeConfig());
        modelBuilder.ApplyConfiguration(new PaymentTransactionEntityTypeConfig());
        modelBuilder.ApplyConfiguration(new InboxEntityTypeConfig());
        modelBuilder.ApplyConfiguration(new OutboxEntityTypeConfig());
    }
}
