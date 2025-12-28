using Microsoft.AspNetCore.Mvc;

namespace OrdersService.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    // “жив ли процесс”
    [HttpGet("live")]
    public IActionResult Live()
        => Ok(new { status = "live" });

    // “готов ли принимать трафик”
    // Пока без тяжёлых проверок, чтобы не зависеть от БД/очередей.
    // При желании можно расширить (проверка подключения к БД, RabbitMQ и т.д.).
    [HttpGet("ready")]
    public IActionResult Ready()
        => Ok(new { status = "ready" });

    // Для совместимости с простым /health
    [HttpGet]
    public IActionResult Root()
        => Ok(new { status = "ok" });
}
