using Microsoft.Extensions.Primitives;
using Swd.Dz4.Contracts.Common;

namespace OrdersService.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HttpContextItemKey = "CorrelationId";

    public async Task Invoke(HttpContext context)
    {
        string correlationId;

        if (context.Request.Headers.TryGetValue(HeaderNames.CorrelationId, out StringValues existing) &&
            !StringValues.IsNullOrEmpty(existing) &&
            !string.IsNullOrWhiteSpace(existing.ToString()))
        {
            correlationId = existing.ToString().Trim();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[HttpContextItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderNames.CorrelationId] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
