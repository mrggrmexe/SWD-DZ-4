using System.Globalization;

namespace Swd.Dz4.Contracts.Common;

/// <summary>
/// Денежная сумма в minor units (копейки/центы).
/// Например: 19999 = 199.99.
/// </summary>
public readonly record struct Money
{
    public const string DefaultCurrency = "RUB";

    public long MinorUnits { get; init; }

    /// <summary>
    /// Код валюты (по умолчанию RUB). Храним как верхний регистр.
    /// </summary>
    public string Currency { get; init; }

    public Money(long minorUnits, string? currency = null)
    {
        MinorUnits = minorUnits;

        var cur = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.Trim().ToUpperInvariant();
        Currency = cur;
    }

    public bool IsZero => MinorUnits == 0;
    public bool IsNegative => MinorUnits < 0;

    public override string ToString() => $"{MinorUnits.ToString(CultureInfo.InvariantCulture)} {Currency}";

    public static Money operator +(Money a, Money b)
        => a.Currency == b.Currency
            ? new Money(checked(a.MinorUnits + b.MinorUnits), a.Currency)
            : throw new InvalidOperationException($"Currency mismatch: {a.Currency} vs {b.Currency}");

    public static Money operator -(Money a, Money b)
        => a.Currency == b.Currency
            ? new Money(checked(a.MinorUnits - b.MinorUnits), a.Currency)
            : throw new InvalidOperationException($"Currency mismatch: {a.Currency} vs {b.Currency}");
}
