using OrdersService.Domain.Enums;

namespace OrdersService.Domain.Entities;

public sealed class Order
{
    public Guid OrderId { get; set; }
    public required string UserId { get; set; }

    public long AmountMinor { get; set; }
    public string? Description { get; set; }

    public OrderStatus Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
