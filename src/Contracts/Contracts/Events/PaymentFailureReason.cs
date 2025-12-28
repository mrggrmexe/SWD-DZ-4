namespace Swd.Dz4.Contracts.Events;

/// <summary>
/// Нормализованные причины отказа оплаты.
/// </summary>
[JsonConverter(typeof(PaymentFailureReasonJsonConverter))]
public enum PaymentFailureReason
{
    Unknown = 0,
    AccountNotFound = 1,
    InsufficientFunds = 2,
    ConcurrencyConflict = 3
}

/// <summary>
/// Конвертер enum со fallback в Unknown — повышает устойчивость при эволюции контрактов.
/// </summary>
public sealed class PaymentFailureReasonJsonConverter : JsonConverter<PaymentFailureReason>
{
    public override PaymentFailureReason Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => TryParseString(reader.GetString()),
                JsonTokenType.Number => TryParseNumber(ref reader),
                _ => PaymentFailureReason.Unknown
            };
        }
        catch
        {
            return PaymentFailureReason.Unknown;
        }
    }

    public override void Write(Utf8JsonWriter writer, PaymentFailureReason value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

    private static PaymentFailureReason TryParseString(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return PaymentFailureReason.Unknown;

        return Enum.TryParse<PaymentFailureReason>(s, ignoreCase: true, out var v)
            ? v
            : PaymentFailureReason.Unknown;
    }

    private static PaymentFailureReason TryParseNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt32(out var i) && Enum.IsDefined(typeof(PaymentFailureReason), i))
            return (PaymentFailureReason)i;

        return PaymentFailureReason.Unknown;
    }
}
