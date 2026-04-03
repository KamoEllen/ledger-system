namespace LedgerSystem.Domain.Exceptions;

public sealed class WalletNotFoundException : DomainException
{
    public Guid WalletId { get; }

    public WalletNotFoundException(Guid walletId)
        : base("WALLET_NOT_FOUND", $"Wallet '{walletId}' was not found.")
    {
        WalletId = walletId;
    }
}
