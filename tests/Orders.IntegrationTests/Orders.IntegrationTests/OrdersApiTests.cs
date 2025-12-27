using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.IntegrationTests.Infrastructure;
using Xunit;

namespace Orders.IntegrationTests;

public sealed class OrdersApiTests : IClassFixture<OrdersPostgresFixture>
{
    private readonly OrdersWebAppFactory _factory;
    private readonly HttpClient _client;

    public OrdersApiTests(OrdersPostgresFixture pg)
    {
        _factory = new OrdersWebAppFactory(pg.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Id", "u-order-1");
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreated()
    {
        var res = await _client.PostAsJsonAsync("/orders", new { amountMinor = 1000, description = "test" });
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await res.Content.ReadFromJsonAsync<CreateOrderResponseDto>();
        body.Should().NotBeNull();
        body!.OrderId.Should().NotBeEmpty();
        body.Status.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Missing_UserId_Header_Returns400()
    {
        using var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/orders", new { amountMinor = 1000, description = "x" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record CreateOrderResponseDto(Guid OrderId, string Status);
}
