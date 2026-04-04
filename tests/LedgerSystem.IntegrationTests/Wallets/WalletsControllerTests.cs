using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.IntegrationTests.Fixtures;

namespace LedgerSystem.IntegrationTests.Wallets;

public sealed class WalletsControllerTests : IntegrationTestBase
{
    public WalletsControllerTests(LedgerApiFactory factory) : base(factory) { }

    // ── Create Wallet ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWallet_ShouldReturn201_WithWalletDetails()
    {
        await AuthenticateAsAsync("wallet-owner@example.com");

        var response = await Client.PostAsJsonAsync("/api/wallets", new { currency = "USD" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var wallet = await response.Content.ReadFromJsonAsync<WalletDto>(JsonOptions);
        wallet.Should().NotBeNull();
        wallet!.Currency.Should().Be("USD");
        wallet.Balance.Should().Be(0m);
        wallet.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWallet_ShouldReturn409_WhenDuplicateCurrency()
    {
        await AuthenticateAsAsync("dupwallet@example.com");

        await Client.PostAsJsonAsync("/api/wallets", new { currency = "EUR" });
        var response = await Client.PostAsJsonAsync("/api/wallets", new { currency = "EUR" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateWallet_ShouldAllowDifferentCurrencies_ForSameUser()
    {
        await AuthenticateAsAsync("multicurrency@example.com");

        var r1 = await Client.PostAsJsonAsync("/api/wallets", new { currency = "USD" });
        var r2 = await Client.PostAsJsonAsync("/api/wallets", new { currency = "EUR" });

        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        r2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateWallet_ShouldReturn400_WhenCurrencyIsInvalid()
    {
        await AuthenticateAsAsync("badcurrency@example.com");

        var response = await Client.PostAsJsonAsync("/api/wallets", new { currency = "INVALID" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWallet_ShouldReturn401_WhenUnauthenticated()
    {
        ClearAuth();
        var response = await Client.PostAsJsonAsync("/api/wallets", new { currency = "USD" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── List Wallets ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWallets_ShouldReturnOnlyOwnWallets()
    {
        // Alice registers and creates 2 wallets
        var aliceToken = await RegisterAndLoginAsync("alice@example.com");
        Authenticate(aliceToken);
        await Client.PostAsJsonAsync("/api/wallets", new { currency = "USD" });
        await Client.PostAsJsonAsync("/api/wallets", new { currency = "EUR" });

        // Bob registers and creates his own wallet
        await AuthenticateAsAsync("bob@example.com");
        await Client.PostAsJsonAsync("/api/wallets", new { currency = "GBP" });

        // Re-authenticate as Alice (already registered — use login)
        var token = await LoginAsync("alice@example.com");
        Authenticate(token);

        var response = await Client.GetAsync("/api/wallets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var wallets = await response.Content.ReadFromJsonAsync<List<WalletDto>>(JsonOptions);
        wallets!.Should().HaveCount(2);
        wallets.Should().AllSatisfy(w => w.Currency.Should().BeOneOf("USD", "EUR"));
    }

    // ── Get Single Wallet ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetWallet_ShouldReturn200_WhenOwnerRequests()
    {
        await AuthenticateAsAsync("getowner@example.com");

        var createResponse = await Client.PostAsJsonAsync("/api/wallets", new { currency = "USD" });
        var created = await createResponse.Content.ReadFromJsonAsync<WalletDto>(JsonOptions);

        var response = await Client.GetAsync($"/api/wallets/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await response.Content.ReadFromJsonAsync<WalletDetailDto>(JsonOptions);
        wallet!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetWallet_ShouldReturn403_WhenAnotherUserRequests()
    {
        // Alice creates a wallet
        await AuthenticateAsAsync("alice2@example.com");
        var createResponse = await Client.PostAsJsonAsync("/api/wallets", new { currency = "USD" });
        var created = await createResponse.Content.ReadFromJsonAsync<WalletDto>(JsonOptions);

        // Bob tries to access Alice's wallet
        await AuthenticateAsAsync("bob2@example.com");
        var response = await Client.GetAsync($"/api/wallets/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetWallet_ShouldReturn404_WhenWalletDoesNotExist()
    {
        await AuthenticateAsAsync("finder@example.com");

        var response = await Client.GetAsync($"/api/wallets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Wallet History ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_ShouldReturn200_WithEmptyHistory_ForNewWallet()
    {
        await AuthenticateAsAsync("history@example.com");

        var createResponse = await Client.PostAsJsonAsync("/api/wallets", new { currency = "USD" });
        var created = await createResponse.Content.ReadFromJsonAsync<WalletDto>(JsonOptions);

        var response = await Client.GetAsync($"/api/wallets/{created!.Id}/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"items\"");
        body.Should().Contain("\"totalCount\":0");
    }

}
