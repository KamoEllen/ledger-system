namespace LedgerSystem.Application.DTOs.Transfers;

/// <summary>
/// Returned on successful POST /api/transfers.
/// Includes the transfer record plus the balances of both wallets immediately after.
/// </summary>
public sealed record TransferResultDto(
    TransferDto Transfer,
    decimal SourceBalanceAfter,
    decimal DestinationBalanceAfter);
