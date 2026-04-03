namespace LedgerSystem.Domain.Exceptions;

public sealed class SelfTransferException : DomainException
{
    public SelfTransferException()
        : base("SELF_TRANSFER", "Source and destination wallets must be different.")
    {
    }
}
