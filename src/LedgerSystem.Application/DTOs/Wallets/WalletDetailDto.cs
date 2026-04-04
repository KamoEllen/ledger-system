namespace LedgerSystem.Application.DTOs.Wallets;

public sealed record WalletDetailDto(
    Guid Id,
    Guid UserId,
    string Currency,
    decimal Balance,
    bool IsActive,
    DateTime CreatedAt);
