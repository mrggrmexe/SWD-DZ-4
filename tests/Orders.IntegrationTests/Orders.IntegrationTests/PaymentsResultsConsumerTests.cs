using System.Net.Http.Json;

using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Orders.IntegrationTests.Infrastructure;
using OrdersService.Domain.Enums;
using OrdersService.Infrastructure.Persistence;
using Swd.Dz4.Contracts.Events;
using Xunit;

namespace Orders.IntegrationTests;

public sealed class PaymentResultsConsumerTests : IClassFixture<OrdersPostgresFixture>
{
    private readonly OrdersWebAppFactory _factory;

    public PaymentResultsConsumerTests(OrdersPostgresFixture pg)
    {
        _factory = new OrdersWebAppFactory(pg.ConnectionString);
        _factory.CreateClient(); // старт host
    }

    [Fact]
    public async Task PaymentSucceededMovesOrderToFinished()
    {
        var userId = "u-order-2";
        var orderId = await CreateOrderAsync(userId, 1500);

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Bus.Publish(new PaymentSucceeded
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Source = EventSources.PaymentsService,
            OrderId = orderId,
            UserId = userId,
            AmountMinor = 1500
        });

        await Eventually.Until(async _ =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.OrderId == orderId);
            return order.Status == OrderStatus.Finished;
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PaymentFailedMovesOrderToCancelled()
    {
        var userId = "u-order-3";
        var orderId = await CreateOrderAsync(userId, 999);

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Bus.Publish(new PaymentFailed
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Source = EventSources.PaymentsService,
            OrderId = orderId,
            UserId = userId,
            AmountMinor = 999,
            Reason = PaymentFailureReason.InsufficientFunds,
            Details = null
        });

        await Eventually.Until(async _ =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.OrderId == orderId);
            return order.Status == OrderStatus.Cancelled;
        }, TimeSpan.FromSeconds(5));
    }

    private async Task<Guid> CreateOrderAsync(string userId, long amountMinor)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId);

        var res = await client.PostAsJsonAsync("/orders", new { amountMinor, description = "integration-test" });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<CreateOrderResponseDto>();
        body.Should().NotBeNull();
        return body!.OrderId;
    }

    private sealed record CreateOrderResponseDto(Guid OrderId, string Status);
}
