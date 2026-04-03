using LedgerSystem.Application.DTOs.Auth;
using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using LedgerSystem.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace LedgerSystem.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;
    private readonly int _refreshTokenExpiryDays;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordService passwordService,
        ITokenService tokenService,
        IUnitOfWork uow,
        IConfiguration config)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _uow = uow;
        _refreshTokenExpiryDays = config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _users.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateEmailException(request.Email);

        var passwordHash = _passwordService.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash, UserRole.User);

        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(request.Email, ct);

        // Use constant-time comparison to prevent timing attacks.
        // Verify even when user is null (dummy hash) to avoid timing differences.
        if (user is null || !_passwordService.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var existing = await _refreshTokens.FindByTokenAsync(request.RefreshToken, ct);

        if (existing is null)
            throw new InvalidRefreshTokenException();

        // Reuse detection: a revoked token being used again is a strong signal
        // that the token was stolen. Revoke everything for this user immediately.
        if (existing.IsRevoked)
        {
            await _refreshTokens.RevokeAllForUserAsync(existing.UserId, ct);
            await _uow.SaveChangesAsync(ct);
            throw new InvalidRefreshTokenException();
        }

        if (!existing.IsActive)
            throw new InvalidRefreshTokenException();

        var user = await _users.FindByIdAsync(existing.UserId, ct);
        if (user is null)
            throw new InvalidRefreshTokenException();

        // Rotate: revoke the old token, issue a new pair
        existing.Revoke();
        await _refreshTokens.UpdateAsync(existing, ct);

        var response = await IssueTokensAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _refreshTokens.FindByTokenAsync(refreshToken, ct);

        if (token is null || token.IsRevoked)
            return; // Idempotent — already logged out

        token.Revoke();
        await _refreshTokens.UpdateAsync(token, ct);
        await _uow.SaveChangesAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(user.Id, rawRefreshToken, _refreshTokenExpiryDays);
        await _refreshTokens.AddAsync(refreshToken, ct);

        var expiresAt = DateTime.UtcNow.AddMinutes(15); // Mirrors JWT expiry in JwtOptions

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            AccessTokenExpiresAt: expiresAt,
            User: new UserDto(user.Id, user.Email, user.Role.ToString()));
    }
}
