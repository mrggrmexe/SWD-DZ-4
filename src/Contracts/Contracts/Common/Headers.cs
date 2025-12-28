namespace Swd.Dz4.Contracts.Common;

/// <summary>
/// Единые имена HTTP-заголовков для всей системы.
/// </summary>
public static class HeaderNames
{
    /// <summary>
    /// Идентификатор пользователя в каждом запросе.
    /// </summary>
    public const string UserId = "X-User-Id";

    /// <summary>
    /// Алиас для совместимости (если где-то остались старые .http/клиенты).
    /// </summary>
    public const string LegacyUserId = "user_id";

    /// <summary>
    /// Корреляция запросов/цепочек событий (удобно для трассировки).
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// Идемпотентность HTTP-операций (например, top-up или создание).
    /// </summary>
    public const string IdempotencyKey = "X-Idempotency-Key";

    /// <summary>
    /// Кандидаты заголовка UserId в порядке приоритета.
    /// </summary>
    public static readonly string[] UserIdCandidates = [UserId, LegacyUserId];
}
