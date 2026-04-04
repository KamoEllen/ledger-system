namespace LedgerSystem.Domain.Exceptions;

/// <summary>
/// Thrown when a user tries to read or operate on a wallet they do not own.
/// Returns 403 Forbidden — not 404 — because the wallet exists but is not accessible.
/// </summary>
public sealed class UnauthorizedWalletAccessException : DomainException
{
    public UnauthorizedWalletAccessException()
        : base("FORBIDDEN", "You do not have access to this wallet.")
    {
    }
}
