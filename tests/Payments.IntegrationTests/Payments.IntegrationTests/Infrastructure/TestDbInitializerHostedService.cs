using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PaymentsService.Infrastructure.Persistence;

namespace Payments.IntegrationTests.Infrastructure;

public sealed class TestDbInitializerHostedService(IServiceScopeFactory scopeFactory, ILogger<TestDbInitializerHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        // Для тестов: быстро создать схему по модели (без миграций)
        await db.Database.EnsureCreatedAsync(cancellationToken);
        logger.LogInformation("Payments test database ensured created.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
