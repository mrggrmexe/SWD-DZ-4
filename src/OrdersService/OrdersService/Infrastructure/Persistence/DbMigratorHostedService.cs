using Microsoft.EntityFrameworkCore;

namespace OrdersService.Infrastructure.Persistence;

/// <summary>
/// Применяет миграции с ретраями (чтобы сервис переживал старт, когда Postgres еще поднимается).
/// Рекомендовано для контейнеров.
/// </summary>
public sealed class DbMigratorHostedService(IServiceScopeFactory scopeFactory, ILogger<DbMigratorHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Не блокируем старт слишком долго: пробуем, если не вышло — ретраи.
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                await db.Database.MigrateAsync(stoppingToken);

                logger.LogInformation("Orders DB migrations applied.");
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
