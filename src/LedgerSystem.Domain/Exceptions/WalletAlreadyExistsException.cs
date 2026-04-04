namespace LedgerSystem.Domain.Exceptions;

public sealed class WalletAlreadyExistsException : DomainException
{
    public WalletAlreadyExistsException(string currency)
        : base("WALLET_ALREADY_EXISTS",
            $"You already have a {currency.ToUpperInvariant()} wallet. Only one wallet per currency is allowed.")
    {
    }
}
