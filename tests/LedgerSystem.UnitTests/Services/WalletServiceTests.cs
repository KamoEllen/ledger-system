using FluentAssertions;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LedgerSystem.UnitTests.Services;

public sealed class WalletServiceTests
{
    private readonly IWalletRepository _walletRepo = Substitute.For<IWalletRepository>();
    private readonly ILedgerEntryRepository _ledgerRepo = Substitute.For<ILedgerEntryRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly WalletService _service;

    public WalletServiceTests()
    {
        _service = new WalletService(_walletRepo, _ledgerRepo, _uow);
    }

    // ── GetByUserIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnMappedDtos()
    {
        var userId = Guid.NewGuid();
        var wallet = Wallet.Create(userId, "USD");
        wallet.Credit(500m);

        _walletRepo.GetByUserIdAsync(userId, default)
            .Returns(new List<Wallet> { wallet });

        var result = await _service.GetByUserIdAsync(userId);

        result.Should().HaveCount(1);
        result[0].Currency.Should().Be("USD");
        result[0].Balance.Should().Be(500m);
        result[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmpty_WhenNoWallets()
    {
        var userId = Guid.NewGuid();
        _walletRepo.GetByUserIdAsync(userId, default)
            .Returns(new List<Wallet>());

        var result = await _service.GetByUserIdAsync(userId);

        result.Should().BeEmpty();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldReturnWallet_WhenOwnerRequests()
    {
        var userId = Guid.NewGuid();
        var wallet = Wallet.Create(userId, "EUR");

        _walletRepo.FindByIdAsync(wallet.Id, default).Returns(wallet);

        var result = await _service.GetByIdAsync(wallet.Id, userId);

        result.Id.Should().Be(wallet.Id);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenWalletNotFound()
    {
        var walletId = Guid.NewGuid();
        _walletRepo.FindByIdAsync(walletId, default).ReturnsNull();

        var act = () => _service.GetByIdAsync(walletId, Guid.NewGuid());

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var wallet = Wallet.Create(ownerId, "USD");
        var differentUserId = Guid.NewGuid();

        _walletRepo.FindByIdAsync(wallet.Id, default).Returns(wallet);

        var act = () => _service.GetByIdAsync(wallet.Id, differentUserId);

        await act.Should().ThrowAsync<UnauthorizedWalletAccessException>();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldCreateAndReturnWallet()
    {
        var userId = Guid.NewGuid();
        _walletRepo.GetByUserIdAsync(userId, default)
            .Returns(new List<Wallet>());

        var result = await _service.CreateAsync(userId, new CreateWalletRequest("USD"));

        result.Currency.Should().Be("USD");
        result.UserId.Should().Be(userId);
        result.Balance.Should().Be(0m);
        result.IsActive.Should().BeTrue();

        await _walletRepo.Received(1).AddAsync(Arg.Any<Wallet>(), default);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDuplicateCurrency()
    {
        var userId = Guid.NewGuid();
        var existingWallet = Wallet.Create(userId, "USD");

        _walletRepo.GetByUserIdAsync(userId, default)
            .Returns(new List<Wallet> { existingWallet });

        var act = () => _service.CreateAsync(userId, new CreateWalletRequest("USD"));

        await act.Should().ThrowAsync<WalletAlreadyExistsException>();
        await _walletRepo.DidNotReceive().AddAsync(Arg.Any<Wallet>(), default);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalisesCurrencyToUppercase()
    {
        var userId = Guid.NewGuid();
        _walletRepo.GetByUserIdAsync(userId, default).Returns(new List<Wallet>());

        var result = await _service.CreateAsync(userId, new CreateWalletRequest("eur"));

        result.Currency.Should().Be("EUR");
    }

    // ── GetHistoryAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_ShouldThrow_WhenNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var wallet = Wallet.Create(ownerId, "USD");

        _walletRepo.FindByIdAsync(wallet.Id, default).Returns(wallet);

        var act = () => _service.GetHistoryAsync(wallet.Id, Guid.NewGuid(), 1, 20);

        await act.Should().ThrowAsync<UnauthorizedWalletAccessException>();
    }
}
