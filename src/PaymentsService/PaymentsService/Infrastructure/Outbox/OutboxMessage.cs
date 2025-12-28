namespace PaymentsService.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid OutboxId { get; set; }

    public Guid MessageId { get; set; }
    public required string MessageType { get; set; }
    public required string Payload { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }

    public string? LockedBy { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }

    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage Create<T>(Guid messageId, T message, DateTimeOffset occurredAtUtc)
        => new()
        {
            OutboxId = Guid.NewGuid(),
            MessageId = messageId,
            MessageType = typeof(T).FullName ?? typeof(T).Name,
            Payload = OutboxSerializer.Serialize(message),
            OccurredAtUtc = occurredAtUtc,
            AttemptCount = 0
        };

    public object Deserialize() => OutboxSerializer.Deserialize(MessageType, Payload);
}
