using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Api.Middleware;
using OrdersService.Infrastructure.Messaging;
using OrdersService.Infrastructure.Outbox;
using OrdersService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
{
    builder.Services.AddSwaggerGen(_ => { });
}

// DbContext
var ordersDb = builder.Configuration.GetConnectionString("OrdersDb");
if (string.IsNullOrWhiteSpace(ordersDb))
    throw new InvalidOperationException("ConnectionStrings:OrdersDb is not configured.");

builder.Services.AddDbContext<OrdersDbContext>(opt =>
{
    opt.UseNpgsql(ordersDb, npgsql =>
    {
        // В проде можно включить RetryOnFailure на уровне провайдера, но для PostgreSQL это не всегда “серебряная пуля”.
        // Оставляем минимально.
    });
});

// Options
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

// MassTransit (RabbitMQ)
builder.Services.AddMassTransit(mt =>
{
    mt.SetKebabCaseEndpointNameFormatter();

    mt.AddConsumer<PaymentResultConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        var opt = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;

        cfg.Host(opt.Host, opt.VirtualHost, h =>
        {
            h.Username(opt.User);
            h.Password(opt.Password);
        });

        cfg.PrefetchCount = (ushort)Math.Clamp(opt.PrefetchCount, 1, 1000);

        // Очередь для результатов оплаты (PaymentSucceeded/PaymentFailed)
        cfg.ReceiveEndpoint(opt.PaymentResultsQueue, e =>
        {
            e.ConfigureConsumer<PaymentResultConsumer>(context);

            // at-least-once: ретраи на транспортном уровне (повторы допустимы)
            e.UseMessageRetry(r =>
            {
                r.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
            });
        });
    });
});

// Hosted services: миграции + outbox publisher
builder.Services.AddHostedService<DbMigratorHostedService>();
builder.Services.AddHostedService<OutboxPublisherHostedService>();

var app = builder.Build();

// Middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<UserIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(_ => { });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
