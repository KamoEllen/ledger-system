using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Wallet>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Locks both wallets in a single query using SELECT FOR UPDATE.
    /// Wallets are always locked in ascending ID order to prevent deadlocks.
    /// </summary>
    Task<(Wallet source, Wallet destination)> LockPairAsync(
        Guid sourceId,
        Guid destinationId,
        CancellationToken ct = default);

    Task AddAsync(Wallet wallet, CancellationToken ct = default);
    Task UpdateAsync(Wallet wallet, CancellationToken ct = default);
}
