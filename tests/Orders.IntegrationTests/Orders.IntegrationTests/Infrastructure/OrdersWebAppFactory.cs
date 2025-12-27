using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OrdersService.Infrastructure.Messaging;
using OrdersService.Infrastructure.Persistence;

namespace Orders.IntegrationTests.Infrastructure;

public sealed class OrdersWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public OrdersWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OrdersDb"] = _connectionString,
                ["Outbox:PollingIntervalMs"] = "50",
                ["Outbox:BatchSize"] = "50",
                ["Outbox:LockSeconds"] = "10",
                ["RabbitMq:Host"] = "unused",
                ["RabbitMq:User"] = "unused",
                ["RabbitMq:Password"] = "unused",
                ["RabbitMq:PaymentResultsQueue"] = "unused"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveByNamespacePrefix("MassTransit");
            RemoveHostedService<OrdersService.Infrastructure.Persistence.DbMigratorHostedService>(services);

            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.SetTestTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
                cfg.AddConsumer<PaymentResultConsumer>();

                cfg.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureEndpoints(context);
                });
            });

            services.AddHostedService<TestDbInitializerHostedService>();
        });
    }

    private static void RemoveHostedService<THosted>(IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var sd = services[i];
            if (sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(THosted))
                services.RemoveAt(i);
        }
    }
}
