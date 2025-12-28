namespace OrdersService.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "rabbitmq";
    public string VirtualHost { get; set; } = "/";
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public int PrefetchCount { get; set; } = 16;

    // Очередь, куда Payments публикует результаты оплаты (на стороне Orders — consumer)
    public string PaymentResultsQueue { get; set; } = "orders.payment-results";
}
