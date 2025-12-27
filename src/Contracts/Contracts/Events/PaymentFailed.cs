namespace Swd.Dz4.Contracts.Events;

/// <summary>
/// Событие: оплата по заказу завершилась ошибкой.
/// </summary>
public sealed record PaymentFailed
{
    public required Guid MessageId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }

    public required DateTimeOffset OccurredAtUtc { get; init; }
    public required string Source { get; init; }
    public int SchemaVersion { get; init; } = ContractSchema.SchemaVersion;

    public required Guid OrderId { get; init; }
    public required string UserId { get; init; }
    public required long AmountMinor { get; init; }

    public required PaymentFailureReason Reason { get; init; }

    /// <summary>
    /// Опциональная детализация (для логов/отладки). Не используй для бизнес-логики.
    /// </summary>
    public string? Details { get; init; }
}
