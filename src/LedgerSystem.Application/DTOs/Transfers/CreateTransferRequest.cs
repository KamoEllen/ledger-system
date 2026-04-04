namespace LedgerSystem.Application.DTOs.Transfers;

public sealed record CreateTransferRequest(
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency,
    string? Description = null);
