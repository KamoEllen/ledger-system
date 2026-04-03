using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerSystem.Infrastructure.Repositories;

public sealed class LedgerEntryRepository : ILedgerEntryRepository
{
    private readonly LedgerDbContext _db;

    public LedgerEntryRepository(LedgerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LedgerEntry>> GetByWalletIdAsync(
        Guid walletId, int page, int pageSize, CancellationToken ct = default) =>
        await _db.LedgerEntries
            .Where(le => le.WalletId == walletId)
            .OrderByDescending(le => le.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <summary>
    /// Returns the wallet balance at a point in time by reading the balance_after
    /// snapshot on the most recent entry up to that timestamp.
    /// O(log n) via the idx_ledger_entries_wallet_created_at index.
    /// </summary>
    public Task<decimal?> GetBalanceAtAsync(
        Guid walletId, DateTime asOf, CancellationToken ct = default) =>
        _db.LedgerEntries
            .Where(le => le.WalletId == walletId && le.CreatedAt <= asOf)
            .OrderByDescending(le => le.CreatedAt)
            .Select(le => (decimal?)le.BalanceAfter)
            .FirstOrDefaultAsync(ct);

    public async Task AddRangeAsync(
        IEnumerable<LedgerEntry> entries, CancellationToken ct = default)
    {
        await _db.LedgerEntries.AddRangeAsync(entries, ct);
    }

    public Task<int> CountByWalletIdAsync(Guid walletId, CancellationToken ct = default) =>
        _db.LedgerEntries.CountAsync(le => le.WalletId == walletId, ct);
}
