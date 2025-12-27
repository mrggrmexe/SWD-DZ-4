using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OrdersService.Infrastructure.Persistence;

namespace Orders.IntegrationTests.Infrastructure;

public sealed class TestDbInitializerHostedService(IServiceScopeFactory scopeFactory, ILogger<TestDbInitializerHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);
        logger.LogInformation("Orders test database ensured created.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
