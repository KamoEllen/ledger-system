using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LedgerSystem.Application.DTOs.Auth;

namespace LedgerSystem.IntegrationTests.Fixtures;

/// <summary>
/// Base class for integration tests.
/// Provides helpers to register/login users and attach JWT tokens to requests.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<LedgerApiFactory>, IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly LedgerApiFactory Factory;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected IntegrationTestBase(LedgerApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth helpers ──────────────────────────────────────────────────────────

    /// <summary>Registers a user and returns their access token.</summary>
    protected async Task<string> RegisterAndLoginAsync(
        string email = "test@example.com",
        string password = "Password1!")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register",
            new { email, password });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    /// <summary>Sets the Authorization header for subsequent requests.</summary>
    protected void Authenticate(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// <summary>Removes the Authorization header.</summary>
    protected void ClearAuth()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>Registers, logs in, and attaches the token to the client.</summary>
    protected async Task AuthenticateAsAsync(string email = "test@example.com", string password = "Password1!")
    {
        var token = await RegisterAndLoginAsync(email, password);
        Authenticate(token);
    }

    /// <summary>Logs in an already-registered user and returns their access token.</summary>
    protected async Task<string> LoginAsync(string email, string password = "Password1!")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return auth!.AccessToken;
    }
}
