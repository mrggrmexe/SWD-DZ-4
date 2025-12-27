using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Api.Middleware;
using PaymentsService.Infrastructure.Messaging;
using PaymentsService.Infrastructure.Outbox;
using PaymentsService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger (компилируется без "null")
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(_ => { });

// DbContext
var dbConn = builder.Configuration.GetConnectionString("PaymentsDb");
if (string.IsNullOrWhiteSpace(dbConn))
    throw new InvalidOperationException("ConnectionStrings:PaymentsDb is not configured.");

builder.Services.AddDbContext<PaymentsDbContext>(opt => opt.UseNpgsql(dbConn));

// Options
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

// MassTransit
builder.Services.AddMassTransit(mt =>
{
    mt.SetKebabCaseEndpointNameFormatter();
    mt.AddConsumer<OrderCreatedConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        var opt = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;

        cfg.Host(opt.Host, opt.VirtualHost, h =>
        {
            h.Username(opt.User);
            h.Password(opt.Password);
        });

        cfg.PrefetchCount = (ushort)Math.Clamp(opt.PrefetchCount, 1, 1000);

        cfg.ReceiveEndpoint(opt.OrderCreatedQueue, e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);

            // at-least-once delivery => retry ok, consumer идемпотентный (Inbox + unique OrderId)
            e.UseMessageRetry(r =>
            {
                r.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
            });
        });
    });
});

// Hosted services
builder.Services.AddHostedService<DbMigratorHostedService>();
builder.Services.AddHostedService<OutboxPublisherHostedService>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<UserIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(_ => { }); // без null и без зависимости от перегрузок
}

app.MapControllers();

app.Run();
