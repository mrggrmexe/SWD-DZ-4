using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersService.Api.Dtos;
using OrdersService.Api.Middleware;
using OrdersService.Domain.Entities;
using OrdersService.Domain.Enums;
using OrdersService.Infrastructure.Outbox;
using OrdersService.Infrastructure.Persistence;
using Swd.Dz4.Contracts.Events;

namespace OrdersService.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrdersDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var userId = HttpContext.Items[UserIdMiddleware.HttpContextItemKey] as string
                     ?? throw new InvalidOperationException("UserId is missing in HttpContext.");

        var correlationId = HttpContext.Items[CorrelationIdMiddleware.HttpContextItemKey] as string;

        if (request.AmountMinor <= 0)
            return BadRequest(new { title = "Invalid amount", detail = "AmountMinor must be > 0" });

        if (request.AmountMinor > 10_000_000_00L)
            return BadRequest(new { title = "Invalid amount", detail = "AmountMinor is too large" });

        var now = DateTimeOffset.UtcNow;
        var orderId = Guid.NewGuid();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var order = new Order
            {
                OrderId = orderId,
                UserId = userId,
                AmountMinor = request.AmountMinor,
                Description = request.Description,
                Status = OrderStatus.New,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Orders.Add(order);

            var messageId = Guid.NewGuid();

            var evt = new OrderCreated
            {
                MessageId = messageId,
                CorrelationId = TryParseGuid(correlationId),
                CausationId = null,
                OccurredAtUtc = now,
                Source = EventSources.OrdersService,
                OrderId = orderId,
                UserId = userId,
                AmountMinor = request.AmountMinor
            };

            var outbox = OutboxMessage.Create(messageId, evt, now);
            db.OutboxMessages.Add(outbox);

            await db.SaveChangesAsync(ct);
            await db.Database.CommitTransactionAsync(ct);

            return CreatedAtAction(nameof(GetById), new { orderId }, new CreateOrderResponse
            {
                OrderId = orderId,
                Status = OrderStatus.New.ToString().ToUpperInvariant()
            });
        }
        catch
        {
            await db.Database.RollbackTransactionAsync(ct);
            throw;
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)
    {
        var userId = HttpContext.Items[UserIdMiddleware.HttpContextItemKey] as string
                     ?? throw new InvalidOperationException("UserId is missing in HttpContext.");

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                AmountMinor = o.AmountMinor,
                Status = o.Status.ToString().ToUpperInvariant(),
                CreatedAtUtc = o.CreatedAtUtc,
                UpdatedAtUtc = o.UpdatedAtUtc,
                Description = o.Description
            })
            .ToListAsync(ct);

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid orderId, CancellationToken ct)
    {
        var userId = HttpContext.Items[UserIdMiddleware.HttpContextItemKey] as string
                     ?? throw new InvalidOperationException("UserId is missing in HttpContext.");

        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId, ct);

        if (order is null)
            return NotFound(new { title = "Order not found" });

        return Ok(new OrderDto
        {
            OrderId = order.OrderId,
            AmountMinor = order.AmountMinor,
            Status = order.Status.ToString().ToUpperInvariant(),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Description = order.Description
        });
    }

    private static Guid? TryParseGuid(string? s) => Guid.TryParse(s, out var g) ? g : null;
}
