using LedgerSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LedgerSystem.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly LedgerDbContext _dbContext;

    public UnitOfWork(LedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _dbContext.SaveChangesAsync(ct);

    public Task BeginTransactionAsync(CancellationToken ct = default) =>
        _dbContext.BeginTransactionAsync(ct);

    public Task CommitTransactionAsync(CancellationToken ct = default) =>
        _dbContext.CommitTransactionAsync(ct);

    public Task RollbackTransactionAsync(CancellationToken ct = default) =>
        _dbContext.RollbackTransactionAsync(ct);

    /// <summary>
    /// Wraps the operation and its transaction inside CreateExecutionStrategy
    /// so that NpgsqlRetryingExecutionStrategy and manual transactions coexist.
    /// The strategy owns the retry loop; the transaction is opened fresh on
    /// each attempt if a transient failure triggers a retry.
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await _dbContext.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
                await _dbContext.CommitTransactionAsync(ct);
                return result;
            }
            catch
            {
                await _dbContext.RollbackTransactionAsync(ct);
                throw;
            }
        });
    }
}