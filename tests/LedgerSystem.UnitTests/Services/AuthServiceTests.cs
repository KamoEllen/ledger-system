using FluentAssertions;
using LedgerSystem.Application.DTOs.Auth;
using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Application.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LedgerSystem.UnitTests.Services;

public sealed class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordService _passwordService = Substitute.For<IPasswordService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();

        _service = new AuthService(
            _users, _refreshTokens, _passwordService, _tokenService, _uow, config);

        // Default: token service returns sensible values
        _tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
        _passwordService.Hash(Arg.Any<string>()).Returns("hashed-password");
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_AndReturnTokens()
    {
        _users.ExistsByEmailAsync("alice@example.com", default).Returns(false);
        _users.FindByEmailAsync("alice@example.com", default).ReturnsNull();

        var result = await _service.RegisterAsync(
            new RegisterRequest("alice@example.com", "Password1!"), default);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");

        await _users.Received(1).AddAsync(Arg.Is<User>(u => u.Email == "alice@example.com"), default);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        _users.ExistsByEmailAsync("alice@example.com", default).Returns(true);

        var act = () => _service.RegisterAsync(
            new RegisterRequest("alice@example.com", "Password1!"), default);

        await act.Should().ThrowAsync<DuplicateEmailException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), default);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        var user = User.Create("bob@example.com", "hashed-pw");
        _users.FindByEmailAsync("bob@example.com", default).Returns(user);
        _passwordService.Verify("Password1!", "hashed-pw").Returns(true);

        var result = await _service.LoginAsync(
            new LoginRequest("bob@example.com", "Password1!"), default);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenUserNotFound()
    {
        _users.FindByEmailAsync("nobody@example.com", default).ReturnsNull();
        _passwordService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = () => _service.LoginAsync(
            new LoginRequest("nobody@example.com", "wrong"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsWrong()
    {
        var user = User.Create("bob@example.com", "hashed-pw");
        _users.FindByEmailAsync("bob@example.com", default).Returns(user);
        _passwordService.Verify("wrong-password", "hashed-pw").Returns(false);

        var act = () => _service.LoginAsync(
            new LoginRequest("bob@example.com", "wrong-password"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenTokenNotFound()
    {
        _refreshTokens.FindByTokenAsync("unknown-token", default).ReturnsNull();

        var act = () => _service.RefreshAsync(
            new RefreshTokenRequest("unknown-token"), default);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenTokenIsRevoked_AndRevokeAll()
    {
        var userId = Guid.NewGuid();
        var revokedToken = RefreshToken.Create(userId, "revoked-token", 7);
        revokedToken.Revoke();

        _refreshTokens.FindByTokenAsync("revoked-token", default).Returns(revokedToken);

        var act = () => _service.RefreshAsync(
            new RefreshTokenRequest("revoked-token"), default);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();

        // Theft detection: all tokens for the user should be revoked
        await _refreshTokens.Received(1).RevokeAllForUserAsync(userId, default);
    }
}
