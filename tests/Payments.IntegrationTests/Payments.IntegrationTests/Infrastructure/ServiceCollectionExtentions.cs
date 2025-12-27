using Microsoft.Extensions.DependencyInjection;

namespace Payments.IntegrationTests.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void RemoveByNamespacePrefix(this IServiceCollection services, string nsPrefix)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var sd = services[i];
            var stNs = sd.ServiceType.Namespace;
            var itNs = sd.ImplementationType?.Namespace;

            if ((stNs is not null && stNs.StartsWith(nsPrefix, StringComparison.Ordinal)) ||
                (itNs is not null && itNs.StartsWith(nsPrefix, StringComparison.Ordinal)))
            {
                services.RemoveAt(i);
            }
        }
    }
}
