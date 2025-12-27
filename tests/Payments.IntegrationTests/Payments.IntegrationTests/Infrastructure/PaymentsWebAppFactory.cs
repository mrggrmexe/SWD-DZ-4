using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using PaymentsService.Infrastructure.Messaging;
using PaymentsService.Infrastructure.Outbox;
using PaymentsService.Infrastructure.Persistence;

namespace Payments.IntegrationTests.Infrastructure;

public sealed class PaymentsWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public PaymentsWebAppFactory(string connectionString)
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
                ["ConnectionStrings:PaymentsDb"] = _connectionString,

                // Ускоряем outbox для тестов
                ["Outbox:PollingIntervalMs"] = "50",
                ["Outbox:BatchSize"] = "50",
                ["Outbox:LockSeconds"] = "10",

                // Эти значения не будут использоваться (мы заменим MassTransit),
                // но пусть лежат, чтобы не было сюрпризов.
                ["RabbitMq:Host"] = "unused",
                ["RabbitMq:User"] = "unused",
                ["RabbitMq:Password"] = "unused",
                ["RabbitMq:OrderCreatedQueue"] = "unused"
            });
        });

        builder.ConfigureServices(services =>
        {
            // 1) Убираем "боевой" MassTransit/RabbitMQ
            services.RemoveByNamespacePrefix("MassTransit");

            // 2) Убираем мигратор (в тестах используем EnsureCreated)
            RemoveHostedService<PaymentsService.Infrastructure.Persistence.DbMigratorHostedService>(services);

            // 3) Добавляем тестовый harness + in-memory транспорт
            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.SetTestTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));

                cfg.AddConsumer<OrderCreatedConsumer>();

                cfg.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureEndpoints(context);
                });
            });

            // 4) Гарантируем схему БД
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
