using Microsoft.AspNetCore.Mvc;

namespace PaymentsService.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "live" });

    [HttpGet("ready")]
    public IActionResult Ready() => Ok(new { status = "ready" });

    [HttpGet]
    public IActionResult Root() => Ok(new { status = "ok" });
}
