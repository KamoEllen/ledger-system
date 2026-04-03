using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.Application.Services;

public sealed class WalletService : IWalletService
{
    private readonly IWalletRepository _wallets;
    private readonly ILedgerEntryRepository _ledgerEntries;
    private readonly IUnitOfWork _uow;

    public WalletService(
        IWalletRepository wallets,
        ILedgerEntryRepository ledgerEntries,
        IUnitOfWork uow)
    {
        _wallets = wallets;
        _ledgerEntries = ledgerEntries;
        _uow = uow;
    }

    public async Task<IReadOnlyList<WalletDto>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var wallets = await _wallets.GetByUserIdAsync(userId, ct);
        return wallets.Select(MapToDto).ToList();
    }

    public async Task<WalletDetailDto> GetByIdAsync(
        Guid walletId, Guid requestingUserId, CancellationToken ct = default)
    {
        var wallet = await _wallets.FindByIdAsync(walletId, ct)
            ?? throw new WalletNotFoundException(walletId);

        GuardOwnership(wallet, requestingUserId);

        return MapToDetailDto(wallet);
    }

    public async Task<WalletDto> CreateAsync(
        Guid userId, CreateWalletRequest request, CancellationToken ct = default)
    {
        var currency = request.Currency.Trim().ToUpperInvariant();

        // One wallet per currency per user
        var existing = await _wallets.GetByUserIdAsync(userId, ct);
        if (existing.Any(w => w.Currency == currency))
            throw new WalletAlreadyExistsException(currency);

        var wallet = Wallet.Create(userId, currency);
        await _wallets.AddAsync(wallet, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToDto(wallet);
    }

    public async Task<PagedResponse<LedgerEntryDto>> GetHistoryAsync(
        Guid walletId,
        Guid requestingUserId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var wallet = await _wallets.FindByIdAsync(walletId, ct)
            ?? throw new WalletNotFoundException(walletId);

        GuardOwnership(wallet, requestingUserId);

        var entries = await _ledgerEntries.GetByWalletIdAsync(walletId, page, pageSize, ct);
        var total = await _ledgerEntries.CountByWalletIdAsync(walletId, ct);

        return new PagedResponse<LedgerEntryDto>(
            entries.Select(MapEntryToDto).ToList(),
            page,
            pageSize,
            total);
    }

    // ── Guards ───────────────────────────────────────────────────────────────

    private static void GuardOwnership(Wallet wallet, Guid requestingUserId)
    {
        if (wallet.UserId != requestingUserId)
            throw new UnauthorizedWalletAccessException();
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static WalletDto MapToDto(Wallet w) =>
        new(w.Id, w.UserId, w.Currency, w.Balance, w.IsActive, w.CreatedAt);

    private static WalletDetailDto MapToDetailDto(Wallet w) =>
        new(w.Id, w.UserId, w.Currency, w.Balance, w.IsActive, w.CreatedAt);

    private static LedgerEntryDto MapEntryToDto(Domain.Entities.LedgerEntry e) =>
        new(e.Id, e.WalletId, e.TransferId, e.EntryType.ToString(),
            e.Amount, e.BalanceAfter, e.Description, e.CreatedAt);
}
