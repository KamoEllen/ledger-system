using LedgerSystem.Domain.Exceptions;
using LedgerSystem.Domain.ValueObjects;

namespace LedgerSystem.Domain.Entities;

public sealed class Wallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Currency { get; private set; }

    /// <summary>
    /// Denormalised balance — kept in sync with ledger entries.
    /// Source of truth for fast balance reads. Ledger entries are the audit trail.
    /// </summary>
    public decimal Balance { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<LedgerEntry> _ledgerEntries = new();
    public IReadOnlyList<LedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    // Required by EF Core
    private Wallet() { Currency = string.Empty; }

    private Wallet(Guid id, Guid userId, string currency, DateTime now)
    {
        Id = id;
        UserId = userId;
        Currency = currency;
        Balance = 0m;
        IsActive = true;
        CreatedAt = now;
    }

    public static Wallet Create(Guid userId, string currencyCode)
    {
        // Validates the currency code via the value object
        var currency = ValueObjects.Currency.From(currencyCode);
        return new Wallet(Guid.NewGuid(), userId, currency.Code, DateTime.UtcNow);
    }

    /// <summary>
    /// Applies a credit (incoming funds). Called by TransferService inside a transaction.
    /// Returns the new balance after the credit.
    /// </summary>
    public decimal Credit(decimal amount)
    {
        GuardActive();
        GuardPositiveAmount(amount);

        Balance += amount;
        return Balance;
    }

    /// <summary>
    /// Applies a debit (outgoing funds). Called by TransferService inside a transaction.
    /// Returns the new balance after the debit.
    /// </summary>
    public decimal Debit(decimal amount)
    {
        GuardActive();
        GuardPositiveAmount(amount);

        if (amount > Balance)
            throw new InsufficientFundsException(Id);

        Balance -= amount;
        return Balance;
    }

    public void Freeze()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Wallet '{Id}' is already frozen.");

        IsActive = false;
    }

    public void Unfreeze()
    {
        if (IsActive)
            throw new InvalidOperationException($"Wallet '{Id}' is already active.");

        IsActive = true;
    }

    private void GuardActive()
    {
        if (!IsActive)
            throw new WalletFrozenException(Id);
    }

    private static void GuardPositiveAmount(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidTransferAmountException(amount);
    }
}
