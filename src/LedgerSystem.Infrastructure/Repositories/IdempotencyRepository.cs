using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerSystem.Infrastructure.Repositories;

public sealed class IdempotencyRepository : IIdempotencyRepository
{
    private readonly LedgerDbContext _db;

    public IdempotencyRepository(LedgerDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns a cached record only if it exists AND has not expired.
    /// An expired key is treated as if it never existed.
    /// </summary>
    public Task<IdempotencyKey?> FindAsync(
        string key, Guid userId, CancellationToken ct = default) =>
        _db.IdempotencyKeys
            .FirstOrDefaultAsync(
                ik => ik.Key == key && ik.UserId == userId && ik.ExpiresAt > DateTime.UtcNow,
                ct);

    public async Task AddAsync(IdempotencyKey idempotencyKey, CancellationToken ct = default)
    {
        await _db.IdempotencyKeys.AddAsync(idempotencyKey, ct);
    }

    /// <summary>
    /// Bulk-deletes expired keys. Called by a scheduled background job.
    /// Uses ExecuteDeleteAsync for a single DELETE statement — no entity loading.
    /// </summary>
    public Task DeleteExpiredAsync(CancellationToken ct = default) =>
        _db.IdempotencyKeys
            .Where(ik => ik.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
}
