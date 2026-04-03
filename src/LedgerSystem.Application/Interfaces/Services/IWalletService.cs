using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Wallets;

namespace LedgerSystem.Application.Interfaces.Services;

public interface IWalletService
{
    Task<IReadOnlyList<WalletDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a wallet by ID. Throws UnauthorizedWalletAccessException if the
    /// requesting user does not own the wallet.
    /// </summary>
    Task<WalletDetailDto> GetByIdAsync(Guid walletId, Guid requestingUserId, CancellationToken ct = default);

    Task<WalletDto> CreateAsync(Guid userId, CreateWalletRequest request, CancellationToken ct = default);

    Task<PagedResponse<LedgerEntryDto>> GetHistoryAsync(
        Guid walletId,
        Guid requestingUserId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
