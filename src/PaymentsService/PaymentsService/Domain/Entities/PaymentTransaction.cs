using PaymentsService.Domain.Enums;

namespace PaymentsService.Domain.Entities;

public sealed class PaymentTransaction
{
    public Guid PaymentTransactionId { get; set; }

    public required Guid OrderId { get; set; }
    public required string UserId { get; set; }
    public long AmountMinor { get; set; }

    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
