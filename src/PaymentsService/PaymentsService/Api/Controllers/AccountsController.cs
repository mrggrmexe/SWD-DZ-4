using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Api.Dtos;
using PaymentsService.Api.Middleware;
using PaymentsService.Domain.Entities;
using PaymentsService.Infrastructure.Persistence;

namespace PaymentsService.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class AccountsController(PaymentsDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAccount(CancellationToken ct)
    {
        var userId = HttpContext.Items[UserIdMiddleware.HttpContextItemKey] as string
                     ?? throw new InvalidOperationException("UserId missing.");

        var exists = await db.Accounts.AnyAsync(a => a.UserId == userId, ct);
        if (exists)
            return Conflict(new { title = "Account already exists" });

        var now = DateTimeOffset.UtcNow;

        db.Accounts.Add(new Account
        {
            UserId = userId,
            BalanceMinor = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await db.SaveChangesAsync(ct);
        return Created(new Uri($"/accounts/balance", UriKind.Relative), new { status = "created" });
    }

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalance(CancellationToken ct)
    {
        var userId = HttpContext.Items[UserIdMiddleware.HttpContextItemKey] as string
                     ?? throw new InvalidOperationException("UserId missing.");

        var acc = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
        if (acc is null)
            return NotFound(new { title = "Account not found" });

        return Ok(new BalanceResponse { UserId = userId, BalanceMinor = acc.BalanceMinor });
    }

    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest request, CancellationToken ct)
    {
        var userId = HttpContext.Items[UserIdMiddleware.HttpContextItemKey] as string
                     ?? throw new InvalidOperationException("UserId missing.");

        if (request.AmountMinor <= 0)
            return BadRequest(new { title = "Invalid amount", detail = "AmountMinor must be > 0" });

        var now = DateTimeOffset.UtcNow;

        // Атомарно увеличиваем баланс (без гонок)
        var rows = await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE accounts
SET balance_minor = balance_minor + {request.AmountMinor},
    updated_at_utc = {now}
WHERE user_id = {userId};
", ct);

        if (rows == 0)
            return NotFound(new { title = "Account not found" });

        return Ok(new { status = "ok" });
    }
}
