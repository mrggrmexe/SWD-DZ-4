using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Domain.Entities;
using PaymentsService.Domain.Enums;
using PaymentsService.Infrastructure.Outbox;
using PaymentsService.Infrastructure.Persistence;
using Swd.Dz4.Contracts.Events;

namespace PaymentsService.Infrastructure.Messaging;

/// <summary>
/// Пытается списать деньги по событию OrderCreated.
/// Идемпотентность:
/// 1) Inbox (по MessageId) защищает от повторной доставки одного и того же сообщения
/// 2) Unique OrderId в PaymentTransactions защищает от повторных заказов/повторной обработки
/// Публикация результата — через Outbox.
/// </summary>
public sealed class OrderCreatedConsumer(PaymentsDbContext db, ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreated>
{
    private const string ConsumerName = "OrderCreatedConsumer";

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var msg = context.Message;
        var now = DateTimeOffset.UtcNow;

        // Вся обработка (inbox + debit + payment_tx + outbox) должна быть атомарной
        await using var tx = await db.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            // 1) Inbox dedup (точно тот же MessageId)
            var alreadyProcessed = await db.InboxMessages.AnyAsync(x => x.MessageId == msg.MessageId, context.CancellationToken);
            if (alreadyProcessed)
            {
                await db.Database.CommitTransactionAsync(context.CancellationToken);
                return;
            }

            db.InboxMessages.Add(new InboxMessage
            {
                InboxId = Guid.NewGuid(),
                MessageId = msg.MessageId,
                Consumer = ConsumerName,
                ProcessedAtUtc = now
            });

            // 2) Если транзакция по OrderId уже есть — просто пере-публикуем результат (не списываем повторно)
            var existingTx = await db.PaymentTransactions.FirstOrDefaultAsync(x => x.OrderId == msg.OrderId, context.CancellationToken);
            if (existingTx is not null)
            {
                await EnqueueResultFromExisting(existingTx, msg, now, context.CancellationToken);
                await db.SaveChangesAsync(context.CancellationToken);
                await db.Database.CommitTransactionAsync(context.CancellationToken);
                return;
            }

            // 3) Списание (атомарное UPDATE с условием balance >= amount)
            var rows = await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE accounts
SET balance_minor = balance_minor - {msg.AmountMinor},
    updated_at_utc = {now}
WHERE user_id = {msg.UserId}
  AND balance_minor >= {msg.AmountMinor};
", context.CancellationToken);

            PaymentStatus status;
            string? failureReason = null;

            if (rows == 1)
            {
                status = PaymentStatus.Succeeded;
            }
            else
            {
                // различаем "нет аккаунта" и "не хватает денег"
                var exists = await db.Accounts.AnyAsync(a => a.UserId == msg.UserId, context.CancellationToken);
                status = PaymentStatus.Failed;
                failureReason = exists ? PaymentFailureReason.InsufficientFunds.ToString() : PaymentFailureReason.AccountNotFound.ToString();
            }

            var paymentTx = new PaymentTransaction
            {
                PaymentTransactionId = Guid.NewGuid(),
                OrderId = msg.OrderId,
                UserId = msg.UserId,
                AmountMinor = msg.AmountMinor,
                Status = status,
                FailureReason = failureReason,
                CreatedAtUtc = now
            };

            db.PaymentTransactions.Add(paymentTx);

            // 4) Outbox result event
            await EnqueueResultFromExisting(paymentTx, msg, now, context.CancellationToken);

            await db.SaveChangesAsync(context.CancellationToken);
            await db.Database.CommitTransactionAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OrderCreated processing failed for OrderId={OrderId}", msg.OrderId);
            await db.Database.RollbackTransactionAsync(context.CancellationToken);
            throw;
        }
    }

    private Task EnqueueResultFromExisting(PaymentTransaction tx, OrderCreated msg, DateTimeOffset now, CancellationToken ct)
    {
        var outboxMessageId = Guid.NewGuid();

        if (tx.Status == PaymentStatus.Succeeded)
        {
            var evt = new PaymentSucceeded
            {
                MessageId = outboxMessageId,
                CorrelationId = msg.CorrelationId,
                CausationId = msg.MessageId,
                OccurredAtUtc = now,
                Source = EventSources.PaymentsService,
                OrderId = msg.OrderId,
                UserId = msg.UserId,
                AmountMinor = msg.AmountMinor
            };

            db.OutboxMessages.Add(OutboxMessage.Create(outboxMessageId, evt, now));
        }
        else
        {
            var reason = Enum.TryParse<PaymentFailureReason>(tx.FailureReason, out var parsed)
                ? parsed
                : PaymentFailureReason.Unknown;

            var evt = new PaymentFailed
            {
                MessageId = outboxMessageId,
                CorrelationId = msg.CorrelationId,
                CausationId = msg.MessageId,
                OccurredAtUtc = now,
                Source = EventSources.PaymentsService,
                OrderId = msg.OrderId,
                UserId = msg.UserId,
                AmountMinor = msg.AmountMinor,
                Reason = reason,
                Details = null
            };

            db.OutboxMessages.Add(OutboxMessage.Create(outboxMessageId, evt, now));
        }

        return Task.CompletedTask;
    }
}
