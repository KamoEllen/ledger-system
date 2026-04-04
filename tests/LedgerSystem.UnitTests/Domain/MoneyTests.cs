using FluentAssertions;
using LedgerSystem.Domain.ValueObjects;

namespace LedgerSystem.UnitTests.Domain;

public sealed class MoneyTests
{
    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Of_ShouldCreateMoney_WithValidInputs()
    {
        var money = Money.Of(100m, "USD");

        money.Amount.Should().Be(100m);
        money.Currency.Code.Should().Be("USD");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Of_ShouldThrow_WhenAmountIsNegative(decimal amount)
    {
        var act = () => Money.Of(amount, "USD");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Of_ShouldAcceptZeroAmount()
    {
        var money = Money.Of(0m, "USD");
        money.Amount.Should().Be(0m);
    }

    // ── Arithmetic ────────────────────────────────────────────────────────────

    [Fact]
    public void Add_ShouldReturnCorrectSum()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(50m, "USD");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
        result.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public void Subtract_ShouldReturnCorrectDifference()
    {
        var a = Money.Of(200m, "USD");
        var b = Money.Of(75m, "USD");

        var result = a.Subtract(b);

        result.Amount.Should().Be(125m);
    }

    [Fact]
    public void Subtract_ShouldThrow_WhenResultWouldBeNegative()
    {
        var a = Money.Of(50m, "USD");
        var b = Money.Of(100m, "USD");

        var act = () => a.Subtract(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Add_ShouldThrow_WhenCurrenciesDiffer()
    {
        var usd = Money.Of(100m, "USD");
        var eur = Money.Of(50m, "EUR");

        var act = () => usd.Add(eur);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_ShouldThrow_WhenCurrenciesDiffer()
    {
        var usd = Money.Of(100m, "USD");
        var eur = Money.Of(50m, "EUR");

        var act = () => usd.Subtract(eur);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_ShouldBeByValue()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equality_ShouldReturnFalse_WhenAmountsDiffer()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(101m, "USD");

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Equality_ShouldReturnFalse_WhenCurrenciesDiffer()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(100m, "EUR");

        a.Should().NotBe(b);
    }

    // ── Comparison ────────────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_ShouldReturnNegative_WhenLessThan()
    {
        var small = Money.Of(10m, "USD");
        var large = Money.Of(100m, "USD");

        small.CompareTo(large).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenEqual()
    {
        var a = Money.Of(50m, "USD");
        var b = Money.Of(50m, "USD");

        a.CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void GreaterThan_ShouldWorkCorrectly()
    {
        var a = Money.Of(200m, "USD");
        var b = Money.Of(100m, "USD");

        (a > b).Should().BeTrue();
        (b > a).Should().BeFalse();
    }
}
