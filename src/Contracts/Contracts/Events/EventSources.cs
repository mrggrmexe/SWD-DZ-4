namespace Swd.Dz4.Contracts.Events;

/// <summary>
/// Единые значения источника события (по смыслу близко к CloudEvents "source").
/// </summary>
public static class EventSources
{
    public const string OrdersService = "orders-service";
    public const string PaymentsService = "payments-service";
}
