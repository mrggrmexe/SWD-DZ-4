using Microsoft.Extensions.Primitives;
using Swd.Dz4.Contracts.Common;

namespace OrdersService.Api.Middleware;

public sealed class UserIdMiddleware(RequestDelegate next)
{
    public const string HttpContextItemKey = "UserId";

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderNames.UserId, out StringValues userIdValues) ||
            StringValues.IsNullOrEmpty(userIdValues))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Missing user id",
                detail = $"Header '{HeaderNames.UserId}' is required."
            });
            return;
        }

        var userId = userIdValues.ToString().Trim();
        if (string.IsNullOrWhiteSpace(userId) || userId.Length > 128)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Invalid user id",
                detail = $"Header '{HeaderNames.UserId}' must be a non-empty string up to 128 chars."
            });
            return;
        }

        context.Items[HttpContextItemKey] = userId;
        await next(context);
    }
}
