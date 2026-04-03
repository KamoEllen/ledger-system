namespace LedgerSystem.Application.Interfaces;

/// <summary>
/// Wraps the database transaction lifecycle.
/// Used by TransferService (M5) to commit wallet balance updates,
/// ledger entries, and transfer status atomically in one round-trip.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
