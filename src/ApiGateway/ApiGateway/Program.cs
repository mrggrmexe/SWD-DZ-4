using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Transforms;

using AspNetIpNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Health checks (сам gateway)
// --------------------
builder.Services.AddHealthChecks();

// --------------------
// YARP reverse proxy + transforms
// --------------------
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        // Пробрасываем X-Correlation-Id в downstream
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            if (transformContext.HttpContext.Items.TryGetValue(Correlation.ItemKey, out var cidObj)
                && cidObj is string cid
                && !string.IsNullOrWhiteSpace(cid))
            {
                // Не дублируем, если клиент уже прислал
                if (!transformContext.ProxyRequest.Headers.Contains(Correlation.HeaderName))
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation(Correlation.HeaderName, cid);
                }
            }

            return ValueTask.CompletedTask;
        });
    });

// Настройка пассивных health-check’ов YARP (быстрее реагирует на падения downstream)
builder.Services.Configure<TransportFailureRateHealthPolicyOptions>(o =>
{
    o.DetectionWindowSize = TimeSpan.FromSeconds(30);
    o.MinimalTotalCountThreshold = 5;
    o.DefaultFailureRateLimit = 0.5; // 50% ошибок за окно => destination unhealthy
});

// --------------------
// Forwarded headers (опционально; важно для .NET 9.0.6+)
// --------------------
var forwardedEnabled =
    builder.Configuration.GetValue("ForwardedHeaders:Enabled", false) ||
    string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);

if (forwardedEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;

        // В .NET 9.0.6+ X-Forwarded-* игнорируются, если прокси не доверенный.
        // Поэтому явно разрешаем private-сети (подходит для docker / internal proxies).
        // В production лучше сузить до конкретных IP вашего ingress/nginx/traefik.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        AddKnownNetwork(options, "127.0.0.0/8");     // loopback
        AddKnownNetwork(options, "::1/128");         // ipv6 loopback
        AddKnownNetwork(options, "10.0.0.0/8");      // private
        AddKnownNetwork(options, "172.16.0.0/12");   // private
        AddKnownNetwork(options, "192.168.0.0/16");  // private

        options.RequireHeaderSymmetry = false;
        options.ForwardLimit = 2;
    });
}

var app = builder.Build();

// --------------------
// Error handling (единый ответ вместо падения пайплайна)
// --------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ApiGateway");

            logger.LogError(feature?.Error, "Unhandled exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync("""
            {"title":"Gateway error","status":500}
            """);
        });
    });
}

// Forwarded headers должны быть как можно раньше в pipeline
if (forwardedEnabled)
{
    app.UseForwardedHeaders();
}

// --------------------
// Correlation ID middleware (стресс-устойчивость для трассировки/логов)
// --------------------
app.Use(async (ctx, next) =>
{
    var cid = GetOrCreateCorrelationId(ctx);
    ctx.Items[Correlation.ItemKey] = cid;

    // Отдаём обратно клиенту — удобно дебажить цепочку
    ctx.Response.OnStarting(() =>
    {
        ctx.Response.Headers[Correlation.HeaderName] = cid;
        return Task.CompletedTask;
    });

    var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ApiGateway");
    using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = cid }))
    {
        await next();
    }
});

// --------------------
// Endpoints
// --------------------
app.MapGet("/", () => Results.Text("ApiGateway is running"));

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Встроенный pipeline MapReverseProxy включает нужные middleware (в т.ч. passive health checks). :contentReference[oaicite:1]{index=1}
app.MapReverseProxy();

app.Run();

// --------------------
// helpers
// --------------------
static string GetOrCreateCorrelationId(HttpContext ctx)
{
    if (ctx.Request.Headers.TryGetValue(Correlation.HeaderName, out StringValues existing) &&
        !StringValues.IsNullOrEmpty(existing))
    {
        var val = existing.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(val))
            return val;
    }

    // Компактный формат для логов/заголовков
    return Guid.NewGuid().ToString("N");
}

static void AddKnownNetwork(ForwardedHeadersOptions options, string cidr)
{
    // Поддержка и IPv4 и IPv6 в одном методе
    var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2) return;

    if (!IPAddress.TryParse(parts[0], out var ip)) return;
    if (!int.TryParse(parts[1], out var prefix)) return;

    options.KnownNetworks.Add(new AspNetIpNetwork(ip, prefix));
}

static class Correlation
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";
}
