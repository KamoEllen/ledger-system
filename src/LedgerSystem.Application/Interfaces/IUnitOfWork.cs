namespace LedgerSystem.Application.Interfaces;

/// <summary>
/// Wraps the database transaction lifecycle.
/// Used by TransferService to commit wallet balance updates,
/// ledger entries, and transfer status atomically in one round-trip.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes <paramref name="operation"/> inside a transaction that is
    /// itself wrapped in the EF execution strategy (e.g. NpgsqlRetryingExecutionStrategy).
    /// Use this instead of BeginTransactionAsync when the operation contains
    /// raw SQL (SELECT FOR UPDATE) that would otherwise conflict with the strategy.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default);
}