using System.Net.Http.Json;

using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Payments.IntegrationTests.Infrastructure;
using PaymentsService.Domain.Enums;
using PaymentsService.Infrastructure.Persistence;
using Swd.Dz4.Contracts.Events;
using Xunit;

namespace Payments.IntegrationTests;

public sealed class OrderCreatedConsumerTests : IClassFixture<PaymentsPostgresFixture>
{
    private readonly PaymentsWebAppFactory _factory;

    public OrderCreatedConsumerTests(PaymentsPostgresFixture pg)
    {
        _factory = new PaymentsWebAppFactory(pg.ConnectionString);
        _factory.CreateClient(); // стартует host
    }

    [Fact]
    public async Task OrderCreated_WithFunds_PublishesPaymentSucceeded_And_DebitsOnce()
    {
        var userId = "u-pay-1";
        var orderId = Guid.NewGuid();

        await SeedAccountAsync(userId, topUpMinor: 10_000);

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var now = DateTimeOffset.UtcNow;

        // publish #1
        await harness.Bus.Publish(new OrderCreated
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            OccurredAtUtc = now,
            Source = EventSources.OrdersService,
            OrderId = orderId,
            UserId = userId,
            AmountMinor = 1_000
        });

        // publish #2 (другая доставка/дубликат, но тот же OrderId)
        await harness.Bus.Publish(new OrderCreated
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            OccurredAtUtc = now,
            Source = EventSources.OrdersService,
            OrderId = orderId,
            UserId = userId,
            AmountMinor = 1_000
        });

        await Eventually.Until(async _ =>
            await harness.Published.Any<PaymentSucceeded>(), TimeSpan.FromSeconds(5));

        // Проверяем: транзакция по orderId одна
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        var txCount = await db.PaymentTransactions.CountAsync(x => x.OrderId == orderId);
        txCount.Should().Be(1);

        var tx = await db.PaymentTransactions.SingleAsync(x => x.OrderId == orderId);
        tx.Status.Should().Be(PaymentStatus.Succeeded);

        // Баланс списали ровно один раз
        var acc = await db.Accounts.SingleAsync(x => x.UserId == userId);
        acc.BalanceMinor.Should().Be(9_000);
    }

    [Fact]
    public async Task OrderCreated_WithoutFunds_PublishesPaymentFailed()
    {
        var userId = "u-pay-2";
        var orderId = Guid.NewGuid();

        await SeedAccountAsync(userId, topUpMinor: 100); // недостаточно

        var harness = _factory.Services.GetRequiredService<ITestHarness>();

        await harness.Bus.Publish(new OrderCreated
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Source = EventSources.OrdersService,
            OrderId = orderId,
            UserId = userId,
            AmountMinor = 1_000
        });

        await Eventually.Until(async _ =>
            await harness.Published.Any<PaymentFailed>(), TimeSpan.FromSeconds(5));
    }

    private async Task SeedAccountAsync(string userId, long topUpMinor)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId);

        await client.PostAsync("/accounts", null);
        await client.PostAsJsonAsync("/accounts/topup", new { amountMinor = topUpMinor });
    }
}
