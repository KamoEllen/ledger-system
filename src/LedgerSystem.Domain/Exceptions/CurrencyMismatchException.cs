namespace LedgerSystem.Domain.Exceptions;

public sealed class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string sourceCurrency, string destinationCurrency)
        : base("CURRENCY_MISMATCH",
            $"Cannot transfer between wallets with different currencies: {sourceCurrency} → {destinationCurrency}.")
    {
    }
}
