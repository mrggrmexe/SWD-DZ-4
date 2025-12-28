using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Domain.Enums;
using OrdersService.Infrastructure.Persistence;
using Swd.Dz4.Contracts.Events;

namespace OrdersService.Infrastructure.Messaging;

/// <summary>
/// Обработка результатов оплаты.
/// Должно быть идемпотентно: повторное событие не должно ломать финальный статус.
/// </summary>
public sealed class PaymentResultConsumer(OrdersDbContext db, ILogger<PaymentResultConsumer> logger) :
    IConsumer<PaymentSucceeded>,
    IConsumer<PaymentFailed>
{
    public Task Consume(ConsumeContext<PaymentSucceeded> context)
        => ApplyResultAsync(context.Message.OrderId, success: true, context.CancellationToken);

    public Task Consume(ConsumeContext<PaymentFailed> context)
        => ApplyResultAsync(context.Message.OrderId, success: false, context.CancellationToken);

    private async Task ApplyResultAsync(Guid orderId, bool success, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order is null)
        {
            logger.LogWarning("Payment result for unknown OrderId={OrderId}", orderId);
            return;
        }

        // Идемпотентность: если уже финальный статус — ничего не делаем
        if (order.Status is OrderStatus.Finished or OrderStatus.Cancelled)
            return;

        if (order.Status != OrderStatus.New)
        {
            // На будущее, если добавишь статусы
            logger.LogInformation("Order {OrderId} in status {Status}, ignoring payment result", orderId, order.Status);
            return;
        }

        order.Status = success ? OrderStatus.Finished : OrderStatus.Cancelled;
        order.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
