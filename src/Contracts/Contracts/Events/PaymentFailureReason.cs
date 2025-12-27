namespace Swd.Dz4.Contracts.Events;

/// <summary>
/// Нормализованные причины отказа оплаты.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentFailureReason
{
    Unknown = 0,
    AccountNotFound = 1,
    InsufficientFunds = 2,
    ConcurrencyConflict = 3
}
