using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;
using LedgerSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerSystem.Infrastructure.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly LedgerDbContext _db;

    public WalletRepository(LedgerDbContext db)
    {
        _db = db;
    }

    public Task<Wallet?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Wallets.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<Wallet>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        await _db.Wallets
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Locks both wallets with SELECT FOR UPDATE in a single query.
    /// Wallets are always locked in ascending UUID order to prevent deadlocks
    /// when two concurrent transfers involve the same pair of wallets.
    /// </summary>
    public async Task<(Wallet source, Wallet destination)> LockPairAsync(
        Guid sourceId, Guid destinationId, CancellationToken ct = default)
    {
        // Deterministic lock order: always lock the lower UUID first.
        // Both concurrent requests T1(A→B) and T2(B→A) will lock A first,
        // then B — so they queue rather than deadlock.
        var orderedIds = new[] { sourceId, destinationId }
            .OrderBy(id => id)
            .ToArray();

        var wallets = await _db.Wallets
            .FromSqlRaw(
                "SELECT * FROM wallets WHERE id = ANY(@p0) FOR UPDATE",
                (object)orderedIds)
            .ToListAsync(ct);

        var source = wallets.FirstOrDefault(w => w.Id == sourceId)
            ?? throw new WalletNotFoundException(sourceId);

        var destination = wallets.FirstOrDefault(w => w.Id == destinationId)
            ?? throw new WalletNotFoundException(destinationId);

        return (source, destination);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken ct = default)
    {
        await _db.Wallets.AddAsync(wallet, ct);
    }

    public Task UpdateAsync(Wallet wallet, CancellationToken ct = default)
    {
        _db.Wallets.Update(wallet);
        return Task.CompletedTask;
    }
}
