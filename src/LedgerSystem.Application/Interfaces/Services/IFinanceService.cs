using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Transfers;
using LedgerSystem.Application.DTOs.Wallets;

namespace LedgerSystem.Application.Interfaces.Services;

/// <summary>
/// Read-only financial reporting surface available to Finance and Admin roles.
/// Unlike the regular TransferService/WalletService, these methods skip ownership
/// checks and return data across all users.
/// </summary>
public interface IFinanceService
{
    /// <summary>Returns all transfers across the system, newest first.</summary>
    Task<PagedResponse<TransferDto>> GetAllTransfersAsync(
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Returns a single transfer by ID regardless of ownership.</summary>
    Task<TransferDto> GetTransferByIdAsync(Guid transferId, CancellationToken ct = default);

    /// <summary>Returns all wallets across the system, newest first.</summary>
    Task<PagedResponse<WalletDto>> GetAllWalletsAsync(
        int page, int pageSize, CancellationToken ct = default);
}
