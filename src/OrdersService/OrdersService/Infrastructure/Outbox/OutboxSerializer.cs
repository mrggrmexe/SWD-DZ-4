using System.Text.Json;
using Swd.Dz4.Contracts.Events;

namespace OrdersService.Infrastructure.Outbox;

public static class OutboxSerializer
{
    // Храним “реестр” типов, чтобы не зависеть от AssemblyQualifiedName
    private static readonly Dictionary<string, Type> KnownTypes = new(StringComparer.Ordinal)
    {
        [typeof(OrderCreated).FullName!] = typeof(OrderCreated)
    };

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public static string Serialize<T>(T message)
        => JsonSerializer.Serialize(message, Options);

    public static object Deserialize(string messageType, string json)
    {
        if (!KnownTypes.TryGetValue(messageType, out var type))
            throw new InvalidOperationException($"Unknown outbox message type: {messageType}");

        return JsonSerializer.Deserialize(json, type, Options)
               ?? throw new InvalidOperationException($"Failed to deserialize outbox message type: {messageType}");
    }
}
