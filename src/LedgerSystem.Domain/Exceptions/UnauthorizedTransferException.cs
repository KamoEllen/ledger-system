namespace LedgerSystem.Domain.Exceptions;

/// <summary>
/// Thrown when the authenticated user tries to transfer from a wallet they do not own.
/// </summary>
public sealed class UnauthorizedTransferException : DomainException
{
    public UnauthorizedTransferException()
        : base("FORBIDDEN", "You do not have permission to transfer from this wallet.")
    {
    }
}
