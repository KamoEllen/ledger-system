using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Transfers;
using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.Application.Services;

public sealed class TransferService : ITransferService
{
    private readonly IWalletRepository _wallets;
    private readonly ITransferRepository _transfers;
    private readonly ILedgerEntryRepository _ledgerEntries;
    private readonly IUnitOfWork _uow;

    public TransferService(
        IWalletRepository wallets,
        ITransferRepository transfers,
        ILedgerEntryRepository ledgerEntries,
        IUnitOfWork uow)
    {
        _wallets = wallets;
        _transfers = transfers;
        _ledgerEntries = ledgerEntries;
        _uow = uow;
    }

    /// <summary>
    /// Executes an atomic double-entry transfer.
    ///
    /// Sequence inside a single DB transaction:
    ///   1. Duplicate idempotency key check (fast path before acquiring locks)
    ///   2. Lock both wallets with SELECT FOR UPDATE in ascending UUID order
    ///      → deterministic lock order prevents deadlocks under concurrent transfers
    ///   3. Validate: requesting user owns source wallet
    ///   4. Validate: source currency matches request currency
    ///   5. Validate: source and destination share the same currency
    ///   6. Create Transfer entity (Pending status)
    ///   7. Debit source wallet → create immutable LedgerEntry (DEBIT, balance_after snapshot)
    ///   8. Credit destination wallet → create immutable LedgerEntry (CREDIT, balance_after snapshot)
    ///   9. Persist everything in one SaveChanges
    ///  10. Mark transfer Completed, persist, commit
    /// Any exception rolls back the entire transaction — no partial state is stored.
    /// </summary>
    public async Task<TransferResultDto> ExecuteAsync(
        TransferRequest request, CancellationToken ct = default)
    {
        // ── 1. Fast duplicate check before acquiring any locks ────────────────
        var existing = await _transfers.FindByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null)
            throw new DuplicateTransferException();

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // ── 2. Lock wallets in ascending UUID order ────────────────────────
            // Both concurrent T1(A→B) and T2(B→A) will always lock the lower
            // UUID first, so they queue rather than deadlock.
            var (source, destination) = await _wallets.LockPairAsync(
                request.SourceWalletId, request.DestinationWalletId, ct);

            // ── 3. Ownership check ────────────────────────────────────────────
            if (source.UserId != request.RequestingUserId)
                throw new UnauthorizedTransferException();

            // ── 4 & 5. Currency validation ────────────────────────────────────
            var currency = request.Currency.Trim().ToUpperInvariant();

            if (source.Currency != currency)
                throw new CurrencyMismatchException(source.Currency, currency);

            if (source.Currency != destination.Currency)
                throw new CurrencyMismatchException(source.Currency, destination.Currency);

            // ── 6. Create transfer record (Pending) ───────────────────────────
            var transfer = Transfer.Create(
                request.SourceWalletId,
                request.DestinationWalletId,
                request.Amount,
                currency,
                request.IdempotencyKey,
                request.Description);

            await _transfers.AddAsync(transfer, ct);

            // ── 7. Debit source (throws InsufficientFundsException if balance low) ──
            var sourceBalanceAfter = source.Debit(request.Amount);
            var debitEntry = LedgerEntry.CreateDebit(
                walletId: source.Id,
                amount: request.Amount,
                balanceAfter: sourceBalanceAfter,
                transferId: transfer.Id,
                description: request.Description);

            // ── 8. Credit destination ─────────────────────────────────────────
            var destBalanceAfter = destination.Credit(request.Amount);
            var creditEntry = LedgerEntry.CreateCredit(
                walletId: destination.Id,
                amount: request.Amount,
                balanceAfter: destBalanceAfter,
                transferId: transfer.Id,
                description: request.Description);

            // ── 9. Persist ledger entries + updated wallet balances ───────────
            await _ledgerEntries.AddRangeAsync(new[] { debitEntry, creditEntry }, ct);
            await _wallets.UpdateAsync(source, ct);
            await _wallets.UpdateAsync(destination, ct);

            // ── 10. Mark completed and commit ─────────────────────────────────
            transfer.MarkCompleted();
            await _transfers.UpdateAsync(transfer, ct);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            return new TransferResultDto(
                MapToDto(transfer),
                sourceBalanceAfter,
                destBalanceAfter);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<TransferDto> GetByIdAsync(
        Guid transferId, Guid requestingUserId, CancellationToken ct = default)
    {
        var transfer = await _transfers.FindByIdAsync(transferId, ct)
            ?? throw new TransferNotFoundException(transferId);

        // Verify the requesting user owns either the source or destination wallet
        var userWallets = await _wallets.GetByUserIdAsync(requestingUserId, ct);
        var userWalletIds = userWallets.Select(w => w.Id).ToHashSet();

        if (!userWalletIds.Contains(transfer.SourceWalletId) &&
            !userWalletIds.Contains(transfer.DestinationWalletId))
            throw new TransferNotFoundException(transferId); // Return 404 not 403 — don't leak existence

        return MapToDto(transfer);
    }

    public async Task<PagedResponse<TransferDto>> GetByUserAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var userWallets = await _wallets.GetByUserIdAsync(userId, ct);
        var walletIds = userWallets.Select(w => w.Id).ToList();

        if (walletIds.Count == 0)
            return new PagedResponse<TransferDto>([], page, pageSize, 0);

        var transfers = await _transfers.GetByWalletIdsAsync(walletIds, page, pageSize, ct);
        var total = await _transfers.CountByWalletIdsAsync(walletIds, ct);

        return new PagedResponse<TransferDto>(
            transfers.Select(MapToDto).ToList(),
            page,
            pageSize,
            total);
    }

    // ── Mapper ───────────────────────────────────────────────────────────────

    private static TransferDto MapToDto(Transfer t) =>
        new(t.Id, t.SourceWalletId, t.DestinationWalletId,
            t.Amount, t.Currency, t.Status.ToString(),
            t.Description, t.IdempotencyKey, t.CreatedAt, t.CompletedAt);
}
