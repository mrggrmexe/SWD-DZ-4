using Microsoft.Extensions.Primitives;
using Swd.Dz4.Contracts.Common;

namespace PaymentsService.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HttpContextItemKey = "CorrelationId";

    public async Task Invoke(HttpContext context)
    {
        string cid;

        if (context.Request.Headers.TryGetValue(HeaderNames.CorrelationId, out StringValues existing) &&
            !StringValues.IsNullOrEmpty(existing) &&
            !string.IsNullOrWhiteSpace(existing.ToString()))
        {
            cid = existing.ToString().Trim();
        }
        else
        {
            cid = Guid.NewGuid().ToString("N");
        }

        context.Items[HttpContextItemKey] = cid;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderNames.CorrelationId] = cid;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
