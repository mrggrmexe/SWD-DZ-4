namespace Swd.Dz4.Contracts.Common;

/// <summary>
/// Единые имена HTTP-заголовков для всей системы.
/// </summary>
public static class HeaderNames
{
    /// <summary>
    /// Идентификатор пользователя в каждом запросе (по ТЗ приходит в каждом запросе).
    /// </summary>
    public const string UserId = "X-User-Id";

    /// <summary>
    /// Корреляция запросов/цепочек событий (удобно для трассировки).
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// (Опционально) идемпотентность HTTP-операций (например, топ-ап или создание).
    /// </summary>
    public const string IdempotencyKey = "X-Idempotency-Key";
}
