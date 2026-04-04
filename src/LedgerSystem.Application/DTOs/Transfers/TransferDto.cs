namespace LedgerSystem.Application.DTOs.Transfers;

public sealed record TransferDto(
    Guid Id,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency,
    string Status,
    string? Description,
    string IdempotencyKey,
    DateTime CreatedAt,
    DateTime? CompletedAt);
