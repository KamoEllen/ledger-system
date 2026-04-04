using FluentAssertions;
using LedgerSystem.Domain.ValueObjects;

namespace LedgerSystem.UnitTests.Domain;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("ZAR")]
    [InlineData("JPY")]
    [InlineData("CHF")]
    public void From_ShouldSucceed_ForValidIso4217Codes(string code)
    {
        var currency = Currency.From(code);
        currency.Code.Should().Be(code);
    }

    [Theory]
    [InlineData("usd")]   // lowercase
    [InlineData("Us")]    // wrong length
    [InlineData("USDD")]  // too long
    [InlineData("U1D")]   // contains digit
    [InlineData("")]      // empty
    [InlineData("   ")]   // whitespace
    public void From_ShouldThrow_ForInvalidCodes(string code)
    {
        var act = () => Currency.From(code);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StaticConstants_ShouldHaveCorrectCodes()
    {
        Currency.USD.Code.Should().Be("USD");
        Currency.EUR.Code.Should().Be("EUR");
        Currency.GBP.Code.Should().Be("GBP");
        Currency.ZAR.Code.Should().Be("ZAR");
    }

    [Fact]
    public void Equality_ShouldBeByValue()
    {
        var a = Currency.From("USD");
        var b = Currency.From("USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equality_ShouldReturnFalse_ForDifferentCurrencies()
    {
        var usd = Currency.From("USD");
        var eur = Currency.From("EUR");

        (usd == eur).Should().BeFalse();
        usd.Should().NotBe(eur);
    }

    [Fact]
    public void ToString_ShouldReturnCode()
    {
        var currency = Currency.From("GBP");
        currency.ToString().Should().Be("GBP");
    }
}
