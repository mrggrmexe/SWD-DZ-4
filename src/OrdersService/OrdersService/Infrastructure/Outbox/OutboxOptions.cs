namespace OrdersService.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public int PollingIntervalMs { get; set; } = 500;
    public int BatchSize { get; set; } = 50;
    public int LockSeconds { get; set; } = 30;
    public int MaxErrorLength { get; set; } = 2000;
}
