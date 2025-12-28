namespace PaymentsService.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "rabbitmq";
    public string VirtualHost { get; set; } = "/";
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public int PrefetchCount { get; set; } = 16;

    public string OrderCreatedQueue { get; set; } = "orders.order-created";
}
