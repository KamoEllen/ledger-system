using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;
using LedgerSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerSystem.IntegrationTests.Fixtures;

/// <summary>
/// SQLite-compatible <see cref="IWalletRepository"/> implementation.
/// Replaces <c>WalletRepository</c>'s PostgreSQL-specific raw SQL
/// (<c>SELECT … FOR UPDATE</c>) with plain EF Core LINQ queries.
/// All other operations delegate to the same EF Core patterns.
/// </summary>
public sealed class TestWalletRepository : IWalletRepository
{
    private readonly LedgerDbContext _db;

    public TestWalletRepository(LedgerDbContext db)
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

    public async Task<IReadOnlyList<Wallet>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default) =>
        await _db.Wallets
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountAllAsync(CancellationToken ct = default) =>
        _db.Wallets.CountAsync(ct);

    /// <summary>
    /// SQLite-safe lock: no SELECT FOR UPDATE, just two LINQ FirstOrDefault queries.
    /// Tests still run inside a real SQLite transaction so the full UoW pipeline
    /// is exercised without deadlock risk.
    /// </summary>
    public async Task<(Wallet source, Wallet destination)> LockPairAsync(
        Guid sourceId, Guid destinationId, CancellationToken ct = default)
    {
        var source = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == sourceId, ct)
            ?? throw new WalletNotFoundException(sourceId);

        var destination = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == destinationId, ct)
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
