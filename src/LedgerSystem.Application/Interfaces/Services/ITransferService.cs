using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Transfers;
using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Services;

public record TransferRequest(
    Guid RequestingUserId,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string? Description = null);

public record TransferResult(
    Transfer Transfer,
    decimal SourceBalanceAfter,
    decimal DestinationBalanceAfter);

public interface ITransferService
{
    /// <summary>
    /// Executes an atomic double-entry transfer inside a serializable transaction.
    /// Wallets are locked in ascending UUID order to prevent deadlocks.
    /// Throws domain exceptions on business rule violations.
    /// </summary>
    Task<TransferResultDto> ExecuteAsync(TransferRequest request, CancellationToken ct = default);

    /// <summary>Returns a transfer visible to the requesting user (owns source or destination wallet).</summary>
    Task<TransferDto> GetByIdAsync(Guid transferId, Guid requestingUserId, CancellationToken ct = default);

    /// <summary>Lists all transfers across all wallets owned by the user.</summary>
    Task<PagedResponse<TransferDto>> GetByUserAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);
}
