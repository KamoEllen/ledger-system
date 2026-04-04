using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LedgerSystem.Application.DTOs.Auth;
using LedgerSystem.Application.DTOs.Transfers;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Infrastructure.Persistence;
using LedgerSystem.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerSystem.IntegrationTests.Transfers;

public sealed class TransfersControllerTests : IntegrationTestBase
{
    public TransfersControllerTests(LedgerApiFactory factory) : base(factory) { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(string token, WalletDto wallet)> CreateUserWithWalletAsync(
        string email, string currency = "USD")
    {
        var regResponse = await Client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password1!" });
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var walletResponse = await Client.PostAsJsonAsync("/api/wallets",
            new { currency });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletDto>(JsonOptions);

        return (auth.AccessToken, wallet!);
    }

    /// <summary>
    /// Directly seeds a wallet balance by calling the seeder or using a raw DB operation.
    /// Because we can't call transfers without an existing balance, we inject funds via
    /// the service layer through a helper scope on the factory.
    /// </summary>
    private async Task SeedWalletBalanceAsync(Guid walletId, decimal amount)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<LedgerSystem.Infrastructure.Persistence.LedgerDbContext>();

        var wallet = await db.Wallets.FindAsync(walletId);
        if (wallet is null) throw new InvalidOperationException($"Wallet {walletId} not found.");

        wallet.Credit(amount);
        await db.SaveChangesAsync();
    }

    // ── Create Transfer ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTransfer_ShouldReturn201_ForValidTransfer()
    {
        var (tokenA, walletA) = await CreateUserWithWalletAsync("sender@example.com");
        var (tokenB, walletB) = await CreateUserWithWalletAsync("receiver@example.com");

        await SeedWalletBalanceAsync(walletA.Id, 500m);

        Authenticate(tokenA);

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Headers = { { "Idempotency-Key", idempotencyKey } },
            Content = JsonContent.Create(new
            {
                sourceWalletId = walletA.Id,
                destinationWalletId = walletB.Id,
                amount = 100m,
                currency = "USD",
                description = "Test transfer"
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<TransferResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Transfer.Amount.Should().Be(100m);
        result.Transfer.Currency.Should().Be("USD");
        result.Transfer.Status.Should().Be("Completed");
        result.SourceBalanceAfter.Should().Be(400m);
        result.DestinationBalanceAfter.Should().Be(100m);
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn400_WhenIdempotencyKeyHeaderMissing()
    {
        var (tokenA, walletA) = await CreateUserWithWalletAsync("sender2@example.com");
        var (_, walletB) = await CreateUserWithWalletAsync("receiver2@example.com");

        await SeedWalletBalanceAsync(walletA.Id, 200m);
        Authenticate(tokenA);

        var response = await Client.PostAsJsonAsync("/api/transfers", new
        {
            sourceWalletId = walletA.Id,
            destinationWalletId = walletB.Id,
            amount = 50m,
            currency = "USD"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn422_WhenInsufficientFunds()
    {
        var (tokenA, walletA) = await CreateUserWithWalletAsync("broke@example.com");
        var (_, walletB) = await CreateUserWithWalletAsync("rich@example.com");

        // Don't seed any balance — wallet starts at 0
        Authenticate(tokenA);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Headers = { { "Idempotency-Key", Guid.NewGuid().ToString() } },
            Content = JsonContent.Create(new
            {
                sourceWalletId = walletA.Id,
                destinationWalletId = walletB.Id,
                amount = 500m,
                currency = "USD"
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn422_WhenSelfTransfer()
    {
        var (tokenA, walletA) = await CreateUserWithWalletAsync("self@example.com");
        await SeedWalletBalanceAsync(walletA.Id, 500m);
        Authenticate(tokenA);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Headers = { { "Idempotency-Key", Guid.NewGuid().ToString() } },
            Content = JsonContent.Create(new
            {
                sourceWalletId = walletA.Id,
                destinationWalletId = walletA.Id,
                amount = 100m,
                currency = "USD"
            })
        };

        var response = await Client.SendAsync(request);

        // SelfTransferException → 422
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTransfer_ShouldReplayResponse_WhenSameIdempotencyKeyUsed()
    {
        var (tokenA, walletA) = await CreateUserWithWalletAsync("idem-sender@example.com");
        var (_, walletB) = await CreateUserWithWalletAsync("idem-receiver@example.com");

        await SeedWalletBalanceAsync(walletA.Id, 1000m);
        Authenticate(tokenA);

        var idempotencyKey = Guid.NewGuid().ToString();

        HttpRequestMessage MakeRequest() =>
            new(HttpMethod.Post, "/api/transfers")
            {
                Headers = { { "Idempotency-Key", idempotencyKey } },
                Content = JsonContent.Create(new
                {
                    sourceWalletId = walletA.Id,
                    destinationWalletId = walletB.Id,
                    amount = 50m,
                    currency = "USD"
                })
            };

        var first = await Client.SendAsync(MakeRequest());
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await Client.SendAsync(MakeRequest());

        // Second response replayed from cache — same 201 status
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        second.Headers.Contains("X-Idempotency-Replayed").Should().BeTrue();
    }

    // ── Get Transfer ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransfer_ShouldReturn200_WhenRequesterOwnsWallet()
    {
        var (tokenA, walletA) = await CreateUserWithWalletAsync("getA@example.com");
        var (_, walletB) = await CreateUserWithWalletAsync("getB@example.com");

        await SeedWalletBalanceAsync(walletA.Id, 300m);
        Authenticate(tokenA);

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Headers = { { "Idempotency-Key", Guid.NewGuid().ToString() } },
            Content = JsonContent.Create(new
            {
                sourceWalletId = walletA.Id,
                destinationWalletId = walletB.Id,
                amount = 30m,
                currency = "USD"
            })
        };

        var created = await Client.SendAsync(createRequest);
        var result = await created.Content.ReadFromJsonAsync<TransferResultDto>(JsonOptions);

        var response = await Client.GetAsync($"/api/transfers/{result!.Transfer.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await response.Content.ReadFromJsonAsync<TransferDto>(JsonOptions);
        transfer!.Id.Should().Be(result.Transfer.Id);
    }

    [Fact]
    public async Task GetTransfer_ShouldReturn404_WhenTransferDoesNotExist()
    {
        await AuthenticateAsAsync("notfound@example.com");

        var response = await Client.GetAsync($"/api/transfers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
