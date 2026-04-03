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
    /// Executes an atomic double-entry transfer.
    /// Idempotency is handled before reaching this method.
    /// Throws domain exceptions on business rule violations.
    /// </summary>
    Task<TransferResult> ExecuteAsync(TransferRequest request, CancellationToken ct = default);
}
