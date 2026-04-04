using FluentAssertions;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.UnitTests.Domain;

public sealed class WalletTests
{
    private static Wallet CreateWallet(decimal initialBalance = 0m)
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        if (initialBalance > 0)
            wallet.Credit(initialBalance);
        return wallet;
    }

    // ── Credit ────────────────────────────────────────────────────────────────

    [Fact]
    public void Credit_ShouldIncreaseBalance()
    {
        var wallet = CreateWallet();

        wallet.Credit(100m);

        wallet.Balance.Should().Be(100m);
    }

    [Fact]
    public void Credit_MultipleTimes_ShouldAccumulateBalance()
    {
        var wallet = CreateWallet();

        wallet.Credit(100m);
        wallet.Credit(50m);
        wallet.Credit(25.50m);

        wallet.Balance.Should().Be(175.50m);
    }

    [Fact]
    public void Credit_ShouldReturnNewBalance()
    {
        var wallet = CreateWallet(initialBalance: 100m);

        var newBalance = wallet.Credit(50m);

        newBalance.Should().Be(150m);
    }

    [Fact]
    public void Credit_ShouldThrow_WhenAmountIsZero()
    {
        var wallet = CreateWallet();
        var act = () => wallet.Credit(0m);

        act.Should().Throw<InvalidTransferAmountException>();
    }

    [Fact]
    public void Credit_ShouldThrow_WhenWalletFrozen()
    {
        var wallet = CreateWallet();
        wallet.Freeze();

        var act = () => wallet.Credit(50m);
        act.Should().Throw<WalletFrozenException>();
    }

    // ── Debit ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Debit_ShouldDecreaseBalance()
    {
        var wallet = CreateWallet(initialBalance: 200m);

        wallet.Debit(50m);

        wallet.Balance.Should().Be(150m);
    }

    [Fact]
    public void Debit_ShouldReturnNewBalance()
    {
        var wallet = CreateWallet(initialBalance: 200m);

        var newBalance = wallet.Debit(75m);

        newBalance.Should().Be(125m);
    }

    [Fact]
    public void Debit_ShouldThrow_WhenInsufficientFunds()
    {
        var wallet = CreateWallet(initialBalance: 100m);

        var act = () => wallet.Debit(200m);
        act.Should().Throw<InsufficientFundsException>();
    }

    [Fact]
    public void Debit_ShouldThrow_WhenExactBalancePlusOne()
    {
        var wallet = CreateWallet(initialBalance: 100m);

        var act = () => wallet.Debit(100.01m);
        act.Should().Throw<InsufficientFundsException>();
    }

    [Fact]
    public void Debit_ShouldSucceed_WhenAmountEqualsBalance()
    {
        var wallet = CreateWallet(initialBalance: 100m);

        var newBalance = wallet.Debit(100m);

        newBalance.Should().Be(0m);
        wallet.Balance.Should().Be(0m);
    }

    [Fact]
    public void Debit_ShouldThrow_WhenAmountIsZero()
    {
        var wallet = CreateWallet(initialBalance: 100m);

        var act = () => wallet.Debit(0m);
        act.Should().Throw<InvalidTransferAmountException>();
    }

    [Fact]
    public void Debit_ShouldThrow_WhenWalletFrozen()
    {
        var wallet = CreateWallet(initialBalance: 100m);
        wallet.Freeze();

        var act = () => wallet.Debit(10m);
        act.Should().Throw<WalletFrozenException>();
    }

    // ── Freeze / Unfreeze ─────────────────────────────────────────────────────

    [Fact]
    public void Freeze_ShouldSetIsActiveToFalse()
    {
        var wallet = CreateWallet();

        wallet.Freeze();

        wallet.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Unfreeze_ShouldSetIsActiveToTrue()
    {
        var wallet = CreateWallet();
        wallet.Freeze();

        wallet.Unfreeze();

        wallet.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Freeze_ThenUnfreeze_ShouldAllowTransactions()
    {
        var wallet = CreateWallet(initialBalance: 100m);
        wallet.Freeze();
        wallet.Unfreeze();

        var newBalance = wallet.Debit(10m);

        newBalance.Should().Be(90m);
    }

    [Fact]
    public void Freeze_ShouldThrow_WhenAlreadyFrozen()
    {
        var wallet = CreateWallet();
        wallet.Freeze();

        var act = () => wallet.Freeze();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unfreeze_ShouldThrow_WhenAlreadyActive()
    {
        var wallet = CreateWallet();

        var act = () => wallet.Unfreeze();
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldInitialiseWithZeroBalance()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "EUR");

        wallet.Balance.Should().Be(0m);
        wallet.Currency.Should().Be("EUR");
        wallet.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldThrow_WhenCurrencyIsInvalid()
    {
        var act = () => Wallet.Create(Guid.NewGuid(), "invalid");
        act.Should().Throw<ArgumentException>();
    }
}
