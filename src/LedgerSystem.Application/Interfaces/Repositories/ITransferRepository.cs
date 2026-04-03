using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Repositories;

public interface ITransferRepository
{
    Task<Transfer?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Transfer>> GetByWalletIdAsync(Guid walletId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Transfer transfer, CancellationToken ct = default);
    Task UpdateAsync(Transfer transfer, CancellationToken ct = default);
}
