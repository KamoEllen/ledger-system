using LedgerSystem.Domain.ValueObjects;

namespace LedgerSystem.UnitTests.Domain;

/// <summary>
/// Unit tests for the Money value object.
/// Full test suite expanded in M8.
/// </summary>
public class MoneyTests
{
    [Fact]
    public void Money_Add_ShouldReturnCorrectSum()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(50m, "USD");

        var result = a.Add(b);

        Assert.Equal(150m, result.Amount);
    }

    [Fact]
    public void Money_Subtract_ShouldReturnCorrectDifference()
    {
        var a = Money.Of(200m, "USD");
        var b = Money.Of(75m, "USD");

        var result = a.Subtract(b);

        Assert.Equal(125m, result.Amount);
    }

    [Fact]
    public void Money_Subtract_ShouldThrow_WhenResultWouldBeNegative()
    {
        var a = Money.Of(50m, "USD");
        var b = Money.Of(100m, "USD");

        Assert.Throws<InvalidOperationException>(() => a.Subtract(b));
    }

    [Fact]
    public void Money_Add_ShouldThrow_WhenCurrenciesDiffer()
    {
        var usd = Money.Of(100m, "USD");
        var eur = Money.Of(50m, "EUR");

        Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
    }

    [Fact]
    public void Money_Of_ShouldThrow_WhenAmountIsNegative()
    {
        Assert.Throws<ArgumentException>(() => Money.Of(-1m, "USD"));
    }

    [Fact]
    public void Money_Equality_ShouldBeByValue()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(100m, "USD");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
