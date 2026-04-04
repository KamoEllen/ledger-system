using FluentAssertions;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;

namespace LedgerSystem.UnitTests.Domain;

public sealed class LedgerEntryTests
{
    private static readonly Guid WalletId = Guid.NewGuid();
    private static readonly Guid TransferId = Guid.NewGuid();

    [Fact]
    public void CreateDebit_ShouldSetCorrectEntryType()
    {
        var entry = LedgerEntry.CreateDebit(WalletId, 100m, 400m, TransferId);

        entry.EntryType.Should().Be(EntryType.Debit);
        entry.Amount.Should().Be(100m);
        entry.BalanceAfter.Should().Be(400m);
        entry.WalletId.Should().Be(WalletId);
        entry.TransferId.Should().Be(TransferId);
    }

    [Fact]
    public void CreateCredit_ShouldSetCorrectEntryType()
    {
        var entry = LedgerEntry.CreateCredit(WalletId, 50m, 550m, TransferId);

        entry.EntryType.Should().Be(EntryType.Credit);
        entry.Amount.Should().Be(50m);
        entry.BalanceAfter.Should().Be(550m);
    }

    [Fact]
    public void CreateDebit_ShouldThrow_WhenAmountIsZero()
    {
        var act = () => LedgerEntry.CreateDebit(WalletId, 0m, 100m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void CreateCredit_ShouldThrow_WhenAmountIsNegative()
    {
        var act = () => LedgerEntry.CreateCredit(WalletId, -50m, 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateDebit_ShouldAssignUniqueId()
    {
        var e1 = LedgerEntry.CreateDebit(WalletId, 10m, 90m);
        var e2 = LedgerEntry.CreateDebit(WalletId, 20m, 70m);

        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void CreateCredit_WithoutTransferId_ShouldHaveNullTransferId()
    {
        var entry = LedgerEntry.CreateCredit(WalletId, 100m, 1100m);

        entry.TransferId.Should().BeNull();
    }

    [Fact]
    public void BalanceAfter_ShouldReflectSnapshotValue()
    {
        // BalanceAfter is a snapshot — it doesn't have to equal Amount
        var entry = LedgerEntry.CreateCredit(WalletId, 100m, balanceAfter: 1500m);

        entry.BalanceAfter.Should().Be(1500m);
        entry.Amount.Should().Be(100m);
    }

    [Fact]
    public void CreatedAt_ShouldBeSetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var entry = LedgerEntry.CreateDebit(WalletId, 10m, 90m);

        entry.CreatedAt.Should().BeOnOrAfter(before);
        entry.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}
