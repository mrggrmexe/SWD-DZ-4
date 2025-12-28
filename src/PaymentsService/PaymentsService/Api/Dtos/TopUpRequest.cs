namespace PaymentsService.Api.Dtos;

public sealed record TopUpRequest
{
    public required long AmountMinor { get; init; }
}
