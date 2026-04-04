using FluentAssertions;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.UnitTests.Domain;

public sealed class TransferTests
{
    private static readonly Guid SourceId = Guid.NewGuid();
    private static readonly Guid DestinationId = Guid.NewGuid();
    private const string IdempotencyKey = "test-key-abc123";

    [Fact]
    public void Create_ShouldSucceed_WithValidInputs()
    {
        var transfer = Transfer.Create(SourceId, DestinationId, 100m, "USD", IdempotencyKey);

        transfer.SourceWalletId.Should().Be(SourceId);
        transfer.DestinationWalletId.Should().Be(DestinationId);
        transfer.Amount.Should().Be(100m);
        transfer.Currency.Should().Be("USD");
        transfer.Status.Should().Be(TransferStatus.Pending);
        transfer.IdempotencyKey.Should().Be(IdempotencyKey);
        transfer.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldThrow_WhenSourceEqualsDestination()
    {
        var sameId = Guid.NewGuid();
        var act = () => Transfer.Create(sameId, sameId, 100m, "USD", IdempotencyKey);

        act.Should().Throw<SelfTransferException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_ShouldThrow_WhenAmountIsNotPositive(decimal amount)
    {
        var act = () => Transfer.Create(SourceId, DestinationId, amount, "USD", IdempotencyKey);

        act.Should().Throw<InvalidTransferAmountException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenIdempotencyKeyIsEmpty()
    {
        var act = () => Transfer.Create(SourceId, DestinationId, 100m, "USD", string.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*idempotency key*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCurrencyIsInvalid()
    {
        var act = () => Transfer.Create(SourceId, DestinationId, 100m, "invalid", IdempotencyKey);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkCompleted_ShouldSetStatusAndTimestamp()
    {
        var transfer = Transfer.Create(SourceId, DestinationId, 50m, "USD", IdempotencyKey);
        var before = DateTime.UtcNow;

        transfer.MarkCompleted();

        transfer.Status.Should().Be(TransferStatus.Completed);
        transfer.CompletedAt.Should().NotBeNull();
        transfer.CompletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkFailed_ShouldSetStatusAndTimestamp()
    {
        var transfer = Transfer.Create(SourceId, DestinationId, 50m, "USD", IdempotencyKey);

        transfer.MarkFailed();

        transfer.Status.Should().Be(TransferStatus.Failed);
        transfer.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_ShouldThrow_WhenAlreadyCompleted()
    {
        var transfer = Transfer.Create(SourceId, DestinationId, 50m, "USD", IdempotencyKey);
        transfer.MarkCompleted();

        var act = () => transfer.MarkCompleted();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkFailed_ShouldThrow_WhenAlreadyFailed()
    {
        var transfer = Transfer.Create(SourceId, DestinationId, 50m, "USD", IdempotencyKey);
        transfer.MarkFailed();

        var act = () => transfer.MarkFailed();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_ShouldAssignNewGuid_ForEachTransfer()
    {
        var t1 = Transfer.Create(SourceId, DestinationId, 10m, "USD", "key-1");
        var t2 = Transfer.Create(SourceId, DestinationId, 20m, "USD", "key-2");

        t1.Id.Should().NotBe(t2.Id);
    }

    [Fact]
    public void Create_ShouldPreserveOptionalDescription()
    {
        var description = "Monthly rent payment";
        var transfer = Transfer.Create(SourceId, DestinationId, 1000m, "USD", IdempotencyKey, description);

        transfer.Description.Should().Be(description);
    }
}
