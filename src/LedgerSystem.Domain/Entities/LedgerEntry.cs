using LedgerSystem.Domain.Enums;

namespace LedgerSystem.Domain.Entities;

/// <summary>
/// Immutable double-entry ledger record.
/// Once created it is NEVER updated or deleted.
/// Any financial correction is made via a compensating entry.
/// </summary>
public sealed class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public Guid? TransferId { get; private set; }
    public EntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>
    /// Snapshot of the wallet balance immediately after this entry was applied.
    /// Enables point-in-time balance queries without replaying all prior entries.
    /// </summary>
    public decimal BalanceAfter { get; private set; }

    public string? Description { get; private set; }

    /// <summary>
    /// No UpdatedAt — this entity is intentionally immutable.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Required by EF Core
    private LedgerEntry() { }

    private LedgerEntry(
        Guid walletId,
        Guid? transferId,
        EntryType entryType,
        decimal amount,
        decimal balanceAfter,
        string? description)
    {
        Id = Guid.NewGuid();
        WalletId = walletId;
        TransferId = transferId;
        EntryType = entryType;
        Amount = amount;
        BalanceAfter = balanceAfter;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public static LedgerEntry CreateDebit(
        Guid walletId,
        decimal amount,
        decimal balanceAfter,
        Guid? transferId = null,
        string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Ledger entry amount must be positive.", nameof(amount));

        return new LedgerEntry(walletId, transferId, EntryType.Debit, amount, balanceAfter, description);
    }

    public static LedgerEntry CreateCredit(
        Guid walletId,
        decimal amount,
        decimal balanceAfter,
        Guid? transferId = null,
        string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Ledger entry amount must be positive.", nameof(amount));

        return new LedgerEntry(walletId, transferId, EntryType.Credit, amount, balanceAfter, description);
    }
}
