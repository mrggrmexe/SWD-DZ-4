namespace PaymentsService.Domain.Entities;

public sealed class Account
{
    // UserId приходит в заголовке -> используем как PK
    public required string UserId { get; set; }

    public long BalanceMinor { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
