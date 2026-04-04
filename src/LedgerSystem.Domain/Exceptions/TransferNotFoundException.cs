namespace LedgerSystem.Domain.Exceptions;

public sealed class TransferNotFoundException : DomainException
{
    public TransferNotFoundException(Guid transferId)
        : base("TRANSFER_NOT_FOUND", $"Transfer '{transferId}' was not found.")
    {
    }
}
