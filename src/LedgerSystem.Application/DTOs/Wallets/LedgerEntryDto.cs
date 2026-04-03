namespace LedgerSystem.Application.DTOs.Wallets;

public sealed record LedgerEntryDto(
    Guid Id,
    Guid WalletId,
    Guid? TransferId,
    string EntryType,
    decimal Amount,
    decimal BalanceAfter,
    string? Description,
    DateTime CreatedAt);
