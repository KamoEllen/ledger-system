namespace LedgerSystem.Domain.Exceptions;

public sealed class InvalidTransferAmountException : DomainException
{
    public InvalidTransferAmountException(decimal amount)
        : base("INVALID_AMOUNT", $"Transfer amount must be greater than zero. Received: {amount}.")
    {
    }
}
