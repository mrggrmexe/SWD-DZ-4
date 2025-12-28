using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 1) HealthChecks (для docker-compose/CI)
builder.Services
    .AddHealthChecks()
    .AddCheck<ReverseProxyConfigHealthCheck>(
        name: "reverse_proxy_config",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" });

// 2) Таймауты (нужны, чтобы Route.Timeout из YARP-конфига реально работал)
builder.Services.AddRequestTimeouts();

// 3) YARP из appsettings.json
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Forwarded headers (опционально, выключено по умолчанию в appsettings)
if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    // В .NET 8.0.17+/9.0.6+ forwarded headers от неизвестных прокси игнорируются,
    // поэтому либо задаём Trusted proxy list, либо явно разрешаем "trust all" (небезопасно для интернета).
    var trustAll = app.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies");

    var options = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                         | ForwardedHeaders.XForwardedProto
                         | ForwardedHeaders.XForwardedHost,
        RequireHeaderSymmetry = false
    };

    if (trustAll)
    {
        // Удобно для Docker/CI, но НЕ рекомендовано для публичного интернета.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }

    app.UseForwardedHeaders(options);
}

// Включаем middleware таймаутов (см. YARP docs)
app.UseRequestTimeouts();

// Удобная “точка” для smoke-check
app.MapGet("/", () => Results.Text("ApiGateway: OK", "text/plain"));

// Liveness: всегда 200
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthJson
});

// Readiness: прогоняем checks (в т.ч. наличие Routes/Clusters)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthJson
});

// Сам reverse proxy
app.MapReverseProxy();

app.Run();

static Task WriteHealthJson(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var payload = new
    {
        status = report.Status.ToString(),
        totalDurationMs = (long)report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            durationMs = (long)e.Value.Duration.TotalMilliseconds
        })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

sealed class ReverseProxyConfigHealthCheck : IHealthCheck
{
    private readonly IProxyConfigProvider _provider;

    public ReverseProxyConfigHealthCheck(IProxyConfigProvider provider)
    {
        _provider = provider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var cfg = _provider.GetConfig();

        var routesCount = cfg.Routes.Count;
        var clustersCount = cfg.Clusters.Count;

        if (routesCount <= 0 || clustersCount <= 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                description: "ReverseProxy config is empty (no routes/clusters). Check appsettings and build output."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            description: $"routes={routesCount}, clusters={clustersCount}"));
    }
}
