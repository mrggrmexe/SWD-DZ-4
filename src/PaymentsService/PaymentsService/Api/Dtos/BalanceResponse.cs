namespace PaymentsService.Api.Dtos;

public sealed record BalanceResponse
{
    public required string UserId { get; init; }
    public required long BalanceMinor { get; init; }
}
