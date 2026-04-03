using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Repositories;

public interface ILedgerEntryRepository
{
    Task<IReadOnlyList<LedgerEntry>> GetByWalletIdAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the wallet balance at a specific point in time by reading the
    /// balance_after snapshot on the most recent ledger entry up to that timestamp.
    /// </summary>
    Task<decimal?> GetBalanceAtAsync(Guid walletId, DateTime asOf, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken ct = default);
}
