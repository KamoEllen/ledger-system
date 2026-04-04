using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Revokes ALL active refresh tokens for a user.
    /// Called when a revoked token is reused (security: token theft detection).
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}
