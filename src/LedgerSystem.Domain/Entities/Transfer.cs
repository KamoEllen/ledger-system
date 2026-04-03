using LedgerSystem.Domain.Enums;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.Domain.Entities;

public sealed class Transfer
{
    public Guid Id { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid DestinationWalletId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public TransferStatus Status { get; private set; }

    /// <summary>
    /// Client-supplied idempotency key. Stored to detect and replay duplicate requests.
    /// </summary>
    public string IdempotencyKey { get; private set; }

    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Required by EF Core
    private Transfer() { Currency = string.Empty; IdempotencyKey = string.Empty; }

    private Transfer(
        Guid id,
        Guid sourceWalletId,
        Guid destinationWalletId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string? description)
    {
        Id = id;
        SourceWalletId = sourceWalletId;
        DestinationWalletId = destinationWalletId;
        Amount = amount;
        Currency = currency;
        Status = TransferStatus.Pending;
        IdempotencyKey = idempotencyKey;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public static Transfer Create(
        Guid sourceWalletId,
        Guid destinationWalletId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string? description = null)
    {
        if (sourceWalletId == destinationWalletId)
            throw new SelfTransferException();

        if (amount <= 0)
            throw new InvalidTransferAmountException(amount);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be empty.", nameof(idempotencyKey));

        // Validates currency code
        ValueObjects.Currency.From(currency);

        return new Transfer(
            Guid.NewGuid(),
            sourceWalletId,
            destinationWalletId,
            amount,
            currency,
            idempotencyKey,
            description);
    }

    public void MarkCompleted()
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot complete a transfer that is already '{Status}'.");

        Status = TransferStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot fail a transfer that is already '{Status}'.");

        Status = TransferStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}
