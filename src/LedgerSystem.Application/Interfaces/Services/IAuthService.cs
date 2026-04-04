using LedgerSystem.Application.DTOs.Auth;

namespace LedgerSystem.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validates the incoming refresh token, revokes it (rotation),
    /// and issues a fresh access + refresh token pair.
    /// If a revoked token is reused, revokes all tokens for that user (theft detection).
    /// </summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
