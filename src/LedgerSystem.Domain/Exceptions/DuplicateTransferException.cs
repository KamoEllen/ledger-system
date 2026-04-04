namespace LedgerSystem.Domain.Exceptions;

/// <summary>
/// Thrown when a transfer with the same idempotency key already exists.
/// Returns HTTP 409 Conflict. M6 will replace this with full response replay.
/// </summary>
public sealed class DuplicateTransferException : DomainException
{
    public DuplicateTransferException()
        : base("DUPLICATE_TRANSFER",
            "A transfer with this idempotency key already exists. " +
            "Use GET /api/transfers/{id} to retrieve the original result.")
    {
    }
}
