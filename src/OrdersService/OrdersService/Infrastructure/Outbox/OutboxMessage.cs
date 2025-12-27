using System.Text.Json;

namespace OrdersService.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid OutboxId { get; set; }

    public Guid MessageId { get; set; }               // дедуп/идемпотентность на стороне consumer
    public required string MessageType { get; set; }  // полное имя типа
    public required string Payload { get; set; }      // JSON

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    // Lease/locking для нескольких инстансов publisher (без глобального мьютекса)
    public string? LockedBy { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }

    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage Create<T>(Guid messageId, T message, DateTimeOffset occurredAtUtc)
    {
        return new OutboxMessage
        {
            OutboxId = Guid.NewGuid(),
            MessageId = messageId,
            MessageType = typeof(T).FullName ?? typeof(T).Name,
            Payload = OutboxSerializer.Serialize(message),
            OccurredAtUtc = occurredAtUtc,
            AttemptCount = 0
        };
    }

    public object Deserialize()
    {
        return OutboxSerializer.Deserialize(MessageType, Payload);
    }
}
