namespace LedgerSystem.Domain.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public Guid WalletId { get; }

    public InsufficientFundsException(Guid walletId)
        : base("INSUFFICIENT_FUNDS", $"Wallet '{walletId}' has insufficient funds for this operation.")
    {
        WalletId = walletId;
    }
}
