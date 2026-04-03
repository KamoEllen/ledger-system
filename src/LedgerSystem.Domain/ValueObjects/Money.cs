namespace LedgerSystem.Domain.ValueObjects;

/// <summary>
/// Represents an exact monetary amount with a currency.
/// Immutable value object — uses decimal (never float) for financial precision.
/// </summary>
public sealed class Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));

        return new Money(amount, currency);
    }

    public static Money Of(decimal amount, string currencyCode) =>
        Of(amount, Currency.From(currencyCode));

    public static Money Zero(Currency currency) => new(0m, currency);

    public Money Add(Money other)
    {
        GuardSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        GuardSameCurrency(other);

        if (other.Amount > Amount)
            throw new InvalidOperationException(
                $"Cannot subtract {other} from {this} — result would be negative.");

        return new Money(Amount - other.Amount, Currency);
    }

    public bool IsGreaterThanOrEqualTo(Money other)
    {
        GuardSameCurrency(other);
        return Amount >= other.Amount;
    }

    public bool IsZero() => Amount == 0m;

    private void GuardSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {Currency} and {other.Currency}.");
    }

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) =>
        obj is Money money && Equals(money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        GuardSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString() => $"{Amount:F4} {Currency}";

    public static bool operator ==(Money? left, Money? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Money? left, Money? right) =>
        !(left == right);

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
}
