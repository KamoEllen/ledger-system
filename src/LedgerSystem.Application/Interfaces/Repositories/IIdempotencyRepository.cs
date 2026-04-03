using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Repositories;

public interface IIdempotencyRepository
{
    /// <summary>
    /// Returns a cached idempotency record if one exists and has not expired.
    /// </summary>
    Task<IdempotencyKey?> FindAsync(string key, Guid userId, CancellationToken ct = default);

    Task AddAsync(IdempotencyKey idempotencyKey, CancellationToken ct = default);

    /// <summary>
    /// Deletes all expired idempotency keys. Called by a background cleanup job.
    /// </summary>
    Task DeleteExpiredAsync(CancellationToken ct = default);
}
