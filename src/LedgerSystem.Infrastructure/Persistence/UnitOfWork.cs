using LedgerSystem.Application.Interfaces;

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
}
