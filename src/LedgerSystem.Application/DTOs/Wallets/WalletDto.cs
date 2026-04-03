namespace LedgerSystem.Application.DTOs.Wallets;

public sealed record WalletDto(
    Guid Id,
    Guid UserId,
    string Currency,
    decimal Balance,
    bool IsActive,
    DateTime CreatedAt);
