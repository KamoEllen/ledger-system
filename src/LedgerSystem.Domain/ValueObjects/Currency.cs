using System.Text.RegularExpressions;

namespace LedgerSystem.Domain.ValueObjects;

/// <summary>
/// ISO 4217 currency code (e.g. USD, EUR, GBP).
/// Immutable value object — equality is by value, not reference.
/// </summary>
public sealed class Currency : IEquatable<Currency>
{
    private static readonly Regex CurrencyCodePattern = new(@"^[A-Z]{3}$", RegexOptions.Compiled);

    public string Code { get; }

    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");
    public static readonly Currency GBP = new("GBP");
    public static readonly Currency ZAR = new("ZAR");

    private Currency(string code)
    {
        Code = code;
    }

    public static Currency From(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be empty.", nameof(code));

        var normalized = code.Trim().ToUpperInvariant();

        if (!CurrencyCodePattern.IsMatch(normalized))
            throw new ArgumentException(
                $"'{code}' is not a valid ISO 4217 currency code. Expected 3 uppercase letters (e.g. USD).",
                nameof(code));

        return new Currency(normalized);
    }

    public bool Equals(Currency? other) =>
        other is not null && Code == other.Code;

    public override bool Equals(object? obj) =>
        obj is Currency currency && Equals(currency);

    public override int GetHashCode() => Code.GetHashCode();

    public override string ToString() => Code;

    public static bool operator ==(Currency? left, Currency? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Currency? left, Currency? right) =>
        !(left == right);
}
