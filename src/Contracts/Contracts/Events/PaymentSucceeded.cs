namespace Swd.Dz4.Contracts.Events;

/// <summary>
/// Событие: оплата по заказу успешно выполнена.
/// </summary>
public sealed record PaymentSucceeded
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
}
