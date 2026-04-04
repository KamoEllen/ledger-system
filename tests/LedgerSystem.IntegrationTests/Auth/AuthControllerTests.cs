using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LedgerSystem.Application.DTOs.Auth;
using LedgerSystem.IntegrationTests.Fixtures;

namespace LedgerSystem.IntegrationTests.Auth;

public sealed class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(LedgerApiFactory factory) : base(factory) { }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ShouldReturn201_WithTokens()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "newuser@example.com",
            password = "SecurePassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Register_ShouldReturn409_WhenEmailAlreadyExists()
    {
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "duplicate@example.com",
            password = "Password1!"
        });

        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "duplicate@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenEmailIsInvalid()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenPasswordIsTooShort()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "short"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ShouldReturn200_WithTokens()
    {
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "logintest@example.com",
            password = "Password1!"
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "logintest@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenPasswordIsWrong()
    {
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "Password1!"
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "user@example.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenUserDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "ghost@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ShouldReturn200_WithNewTokens()
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "refresh@example.com",
            password = "Password1!"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = auth!.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBe(auth.RefreshToken); // token rotated
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WithInvalidToken()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "not-a-real-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ShouldReturn204_AndRevokeToken()
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "logout@example.com",
            password = "Password1!"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        Authenticate(auth!.AccessToken);

        var logoutResponse = await Client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = auth.RefreshToken
        });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Token should now be invalid for refresh
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = auth.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Protected route requires auth ─────────────────────────────────────────

    [Fact]
    public async Task ProtectedRoute_ShouldReturn401_WithoutToken()
    {
        ClearAuth();
        var response = await Client.GetAsync("/api/wallets");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
