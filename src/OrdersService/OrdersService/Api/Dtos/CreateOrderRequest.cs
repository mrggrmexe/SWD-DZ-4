namespace OrdersService.Api.Dtos;

public sealed record CreateOrderRequest
{
    public required long AmountMinor { get; init; }
    public string? Description { get; init; }
}
