using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Payments.IntegrationTests.Infrastructure;
using Xunit;

namespace Payments.IntegrationTests;

public sealed class AccountsApiTests : IClassFixture<PaymentsPostgresFixture>
{
    private readonly PaymentsWebAppFactory _factory;
    private readonly HttpClient _client;

    public AccountsApiTests(PaymentsPostgresFixture pg)
    {
        _factory = new PaymentsWebAppFactory(pg.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Id", "u-test-1");
    }

    [Fact]
    public async Task CreateAccount_Then_BalanceIsZero()
    {
        var create = await _client.PostAsync("/accounts", content: null);
        create.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);

        var balance = await _client.GetFromJsonAsync<BalanceDto>("/accounts/balance");
        balance.Should().NotBeNull();
        balance!.BalanceMinor.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task TopUp_IncreasesBalance()
    {
        await _client.PostAsync("/accounts", null);

        var topup = await _client.PostAsJsonAsync("/accounts/topup", new { amountMinor = 12345 });
        topup.StatusCode.Should().Be(HttpStatusCode.OK);

        var balance = await _client.GetFromJsonAsync<BalanceDto>("/accounts/balance");
        balance!.BalanceMinor.Should().BeGreaterOrEqualTo(12345);
    }

    [Fact]
    public async Task Missing_UserId_Header_Returns400()
    {
        using var client = _factory.CreateClient(); // без заголовка
        var res = await client.GetAsync("/accounts/balance");
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record BalanceDto(string UserId, long BalanceMinor);
}
