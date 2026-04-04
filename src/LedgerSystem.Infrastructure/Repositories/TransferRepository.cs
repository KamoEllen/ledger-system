using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerSystem.Infrastructure.Repositories;

public sealed class TransferRepository : ITransferRepository
{
    private readonly LedgerDbContext _db;

    public TransferRepository(LedgerDbContext db)
    {
        _db = db;
    }

    public Task<Transfer?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Transfers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Transfer?> FindByIdempotencyKeyAsync(string key, CancellationToken ct = default) =>
        _db.Transfers.FirstOrDefaultAsync(t => t.IdempotencyKey == key, ct);

    public async Task<IReadOnlyList<Transfer>> GetByWalletIdsAsync(
        IEnumerable<Guid> walletIds, int page, int pageSize, CancellationToken ct = default)
    {
        var ids = walletIds.ToList();
        return await _db.Transfers
            .Where(t => ids.Contains(t.SourceWalletId) || ids.Contains(t.DestinationWalletId))
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountByWalletIdsAsync(IEnumerable<Guid> walletIds, CancellationToken ct = default)
    {
        var ids = walletIds.ToList();
        return _db.Transfers
            .CountAsync(t => ids.Contains(t.SourceWalletId) || ids.Contains(t.DestinationWalletId), ct);
    }

    public async Task<IReadOnlyList<Transfer>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default) =>
        await _db.Transfers
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountAllAsync(CancellationToken ct = default) =>
        _db.Transfers.CountAsync(ct);

    public async Task AddAsync(Transfer transfer, CancellationToken ct = default)
    {
        await _db.Transfers.AddAsync(transfer, ct);
    }

    public Task UpdateAsync(Transfer transfer, CancellationToken ct = default)
    {
        _db.Transfers.Update(transfer);
        return Task.CompletedTask;
    }
}
