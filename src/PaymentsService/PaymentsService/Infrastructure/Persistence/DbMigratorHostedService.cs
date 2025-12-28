using Microsoft.EntityFrameworkCore;

namespace PaymentsService.Infrastructure.Persistence;

public sealed class DbMigratorHostedService(IServiceScopeFactory scopeFactory, ILogger<DbMigratorHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                await db.Database.MigrateAsync(stoppingToken);

                logger.LogInformation("Payments DB migrations applied.");
                return;
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, 2 * attempt));
                logger.LogWarning(ex, "Failed to apply migrations (attempt {Attempt}). Retrying in {Delay}.", attempt, delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
