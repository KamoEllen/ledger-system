using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.UnitTests.Domain;

/// <summary>
/// Unit tests for Wallet domain entity.
/// Verifies business rules: debit, credit, freeze, and balance guards.
/// Full test suite expanded in M8.
/// </summary>
public class WalletTests
{
    private static Wallet CreateWallet(decimal initialBalance = 0m)
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");

        if (initialBalance > 0)
            wallet.Credit(initialBalance);

        return wallet;
    }

    [Fact]
    public void Credit_ShouldIncreaseBalance()
    {
        var wallet = CreateWallet();

        wallet.Credit(100m);

        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Debit_ShouldDecreaseBalance()
    {
        var wallet = CreateWallet(initialBalance: 200m);

        wallet.Debit(50m);

        Assert.Equal(150m, wallet.Balance);
    }

    [Fact]
    public void Debit_ShouldThrow_WhenInsufficientFunds()
    {
        var wallet = CreateWallet(initialBalance: 100m);

        Assert.Throws<InsufficientFundsException>(() => wallet.Debit(200m));
    }

    [Fact]
    public void Debit_ShouldThrow_WhenWalletFrozen()
    {
        var wallet = CreateWallet(initialBalance: 100m);
        wallet.Freeze();

        Assert.Throws<WalletFrozenException>(() => wallet.Debit(10m));
    }

    [Fact]
    public void Credit_ShouldThrow_WhenWalletFrozen()
    {
        var wallet = CreateWallet();
        wallet.Freeze();

        Assert.Throws<WalletFrozenException>(() => wallet.Credit(50m));
    }

    [Fact]
    public void Debit_ShouldThrow_WhenAmountIsZero()
    {
        var wallet = CreateWallet(initialBalance: 100m);

        Assert.Throws<InvalidTransferAmountException>(() => wallet.Debit(0m));
    }

    [Fact]
    public void Freeze_ThenUnfreeze_ShouldAllowTransactions()
    {
        var wallet = CreateWallet(initialBalance: 100m);
        wallet.Freeze();
        wallet.Unfreeze();

        var balanceAfter = wallet.Debit(10m);

        Assert.Equal(90m, balanceAfter);
    }
}
