using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Repositories;

public interface ITransferRepository
{
    Task<Transfer?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Transfer?> FindByIdempotencyKeyAsync(string key, CancellationToken ct = default);

    /// <summary>Returns transfers where any of the given wallet IDs is source or destination.</summary>
    Task<IReadOnlyList<Transfer>> GetByWalletIdsAsync(
        IEnumerable<Guid> walletIds, int page, int pageSize, CancellationToken ct = default);

    Task<int> CountByWalletIdsAsync(IEnumerable<Guid> walletIds, CancellationToken ct = default);

    Task AddAsync(Transfer transfer, CancellationToken ct = default);
    Task UpdateAsync(Transfer transfer, CancellationToken ct = default);
}
