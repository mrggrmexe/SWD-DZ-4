namespace Swd.Dz4.Contracts.Common;

/// <summary>
/// Денежная сумма в minor units (копейки/центы).
/// Например: 19999 = 199.99.
/// </summary>
public readonly record struct Money(long MinorUnits, string Currency = "RUB")
{
    public const string DefaultCurrency = "RUB";
}
