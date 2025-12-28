namespace PaymentsService.Infrastructure.Outbox;

/// <summary>
/// Inbox для идемпотентной обработки входящих сообщений (OrderCreated).
/// </summary>
public sealed class InboxMessage
{
    public Guid InboxId { get; set; }

    public Guid MessageId { get; set; }           // уникальный id входящего сообщения
    public required string Consumer { get; set; } // имя consumer'а (на будущее)

    public DateTimeOffset ProcessedAtUtc { get; set; }
}
