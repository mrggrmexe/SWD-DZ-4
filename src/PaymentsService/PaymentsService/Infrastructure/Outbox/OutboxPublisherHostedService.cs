using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentsService.Infrastructure.Persistence;

namespace PaymentsService.Infrastructure.Outbox;

public sealed class OutboxPublisherHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    private readonly string _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
    private readonly OutboxOptions _opt = outboxOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(Math.Max(50, _opt.PollingIntervalMs));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await PublishBatchAsync(stoppingToken);
                if (published == 0)
                    await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publisher loop error");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task<int> PublishBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var now = DateTimeOffset.UtcNow;
        var lockUntil = now.AddSeconds(Math.Clamp(_opt.LockSeconds, 5, 300));
        var batchSize = Math.Clamp(_opt.BatchSize, 1, 500);

        List<OutboxMessage> batch;

        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            batch = await db.OutboxMessages
                .FromSqlInterpolated($@"
SELECT *
FROM outbox_messages
WHERE sent_at_utc IS NULL
  AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= {now})
  AND (locked_until_utc IS NULL OR locked_until_utc < {now})
ORDER BY occurred_at_utc
LIMIT {batchSize}
FOR UPDATE SKIP LOCKED
")
                .ToListAsync(ct);

            if (batch.Count == 0)
            {
                await db.Database.CommitTransactionAsync(ct);
                return 0;
            }

            foreach (var msg in batch)
            {
                msg.LockedBy = _instanceId;
                msg.LockedUntilUtc = lockUntil;
                msg.AttemptCount++;
            }

            await db.SaveChangesAsync(ct);
            await db.Database.CommitTransactionAsync(ct);
        }

        var successCount = 0;

        foreach (var msg in batch)
        {
            try
            {
                var obj = msg.Deserialize();
                await publishEndpoint.Publish(obj, obj.GetType(), ct);

                await MarkSentAsync(msg.OutboxId, ct);
                successCount++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish outbox message {OutboxId} type={Type} attempt={Attempt}",
                    msg.OutboxId, msg.MessageType, msg.AttemptCount);

                await ScheduleRetryAsync(msg.OutboxId, msg.AttemptCount, ex, ct);
            }
        }

        return successCount;
    }

    private async Task MarkSentAsync(Guid outboxId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        var msg = await db.OutboxMessages.FirstOrDefaultAsync(x => x.OutboxId == outboxId, ct);
        if (msg is null) return;

        msg.SentAtUtc = DateTimeOffset.UtcNow;
        msg.LockedBy = null;
        msg.LockedUntilUtc = null;
        msg.NextAttemptAtUtc = null;
        msg.LastError = null;

        await db.SaveChangesAsync(ct);
    }

    private async Task ScheduleRetryAsync(Guid outboxId, int attempt, Exception ex, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        var msg = await db.OutboxMessages.FirstOrDefaultAsync(x => x.OutboxId == outboxId, ct);
        if (msg is null) return;

        var now = DateTimeOffset.UtcNow;
        var backoffSeconds = Math.Min(60, (int)Math.Pow(2, Math.Min(attempt, 6)));
        msg.NextAttemptAtUtc = now.AddSeconds(backoffSeconds);

        msg.LockedBy = null;
        msg.LockedUntilUtc = null;

        var maxLen = Math.Clamp(_opt.MaxErrorLength, 200, 10_000);
        var err = ex.ToString();
        msg.LastError = err.Length <= maxLen ? err : err[..maxLen];

        await db.SaveChangesAsync(ct);
    }
}
