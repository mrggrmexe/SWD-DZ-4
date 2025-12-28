namespace Orders.IntegrationTests.Infrastructure;

public static class Eventually
{
    public static async Task Until(
        Func<CancellationToken, Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? poll = null,
        CancellationToken ct = default)
    {
        var interval = poll ?? TimeSpan.FromMilliseconds(100);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        while (!cts.IsCancellationRequested)
        {
            if (await condition(cts.Token))
                return;

            await Task.Delay(interval, cts.Token);
        }

        throw new TimeoutException($"Condition not met within {timeout}.");
    }
}
