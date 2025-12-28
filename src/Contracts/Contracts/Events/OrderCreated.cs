namespace Swd.Dz4.Contracts.Events;

/// <summary>
/// Событие: заказ создан, требуется запустить оплату.
/// </summary>
public sealed record OrderCreated
{
    /// <summary>Уникальный ID сообщения (для inbox/dedup).</summary>
    public required Guid MessageId { get; init; }

    /// <summary>Корреляция цепочки (HTTP -> OrderCreated -> PaymentResult).</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Причина (какое сообщение породило текущее), помогает при трассировке.</summary>
    public Guid? CausationId { get; init; }

    /// <summary>Когда событие произошло (UTC).</summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>Источник события (например, orders-service).</summary>
    public required string Source { get; init; }

    /// <summary>Версия схемы контракта.</summary>
    public int SchemaVersion { get; init; } = ContractSchema.SchemaVersion;

    /// <summary>Бизнес-ключ заказа.</summary>
    public required Guid OrderId { get; init; }

    /// <summary>ID пользователя (как строка — устойчиво к разным форматам user_id).</summary>
    public required string UserId { get; init; }

    /// <summary>Сумма заказа в minor units (копейки/центы).</summary>
    public required long AmountMinor { get; init; }
}
