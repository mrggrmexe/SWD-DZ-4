namespace OrdersService.Api.Dtos;

public sealed record OrderDto
{
    public required Guid OrderId { get; init; }
    public required long AmountMinor { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public string? Description { get; init; }
}
