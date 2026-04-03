namespace LedgerSystem.Domain.Exceptions;

public sealed class WalletFrozenException : DomainException
{
    public Guid WalletId { get; }

    public WalletFrozenException(Guid walletId)
        : base("WALLET_FROZEN", $"Wallet '{walletId}' is frozen and cannot be used for transactions.")
    {
        WalletId = walletId;
    }
}
