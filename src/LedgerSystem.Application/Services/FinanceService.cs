using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Transfers;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.Application.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly ITransferRepository _transfers;
    private readonly IWalletRepository _wallets;

    public FinanceService(ITransferRepository transfers, IWalletRepository wallets)
    {
        _transfers = transfers;
        _wallets = wallets;
    }

    public async Task<PagedResponse<TransferDto>> GetAllTransfersAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var transfers = await _transfers.GetAllAsync(page, pageSize, ct);
        var total = await _transfers.CountAllAsync(ct);
        var items = transfers.Select(MapTransfer).ToList();
        return new PagedResponse<TransferDto>(items, page, pageSize, total);
    }

    public async Task<TransferDto> GetTransferByIdAsync(
        Guid transferId, CancellationToken ct = default)
    {
        var transfer = await _transfers.FindByIdAsync(transferId, ct)
            ?? throw new TransferNotFoundException(transferId);

        return MapTransfer(transfer);
    }

    public async Task<PagedResponse<WalletDto>> GetAllWalletsAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var wallets = await _wallets.GetAllAsync(page, pageSize, ct);
        var total = await _wallets.CountAllAsync(ct);
        var items = wallets.Select(MapWallet).ToList();
        return new PagedResponse<WalletDto>(items, page, pageSize, total);
    }

    private static TransferDto MapTransfer(Transfer t) =>
        new(t.Id, t.SourceWalletId, t.DestinationWalletId, t.Amount, t.Currency,
            t.Status.ToString(), t.Description, t.IdempotencyKey, t.CreatedAt, t.CompletedAt);

    private static WalletDto MapWallet(Wallet w) =>
        new(w.Id, w.UserId, w.Currency, w.Balance, w.IsActive, w.CreatedAt);
}
