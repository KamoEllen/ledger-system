using FluentAssertions;
using LedgerSystem.Application.DTOs.Admin;
using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using LedgerSystem.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LedgerSystem.UnitTests.Services;

public sealed class AdminServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWalletRepository _wallets = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _service = new AdminService(_users, _wallets, _uow);
    }

    // ── GetUsersAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPaginatedUsers()
    {
        var users = new List<User>
        {
            User.Create("alice@example.com", "hash1"),
            User.Create("bob@example.com", "hash2")
        };

        _users.GetAllAsync(1, 20, default).Returns(users);
        _users.CountAllAsync(default).Returns(2);

        var result = await _service.GetUsersAsync(1, 20);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(1);
    }

    // ── GetUserByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
    {
        var user = User.Create("alice@example.com", "hash");
        _users.FindByIdAsync(user.Id, default).Returns(user);

        var result = await _service.GetUserByIdAsync(user.Id);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be("alice@example.com");
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldThrow_WhenNotFound()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, default).ReturnsNull();

        var act = () => _service.GetUserByIdAsync(userId);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    // ── UpdateUserRoleAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserRoleAsync_ShouldUpdateRole_WhenValid()
    {
        var user = User.Create("alice@example.com", "hash");
        _users.FindByIdAsync(user.Id, default).Returns(user);

        var result = await _service.UpdateUserRoleAsync(user.Id, new UpdateUserRoleRequest("Finance"));

        result.Role.Should().Be("Finance");
        user.Role.Should().Be(UserRole.Finance);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ShouldThrow_WhenRoleIsInvalid()
    {
        var user = User.Create("alice@example.com", "hash");
        _users.FindByIdAsync(user.Id, default).Returns(user);

        var act = () => _service.UpdateUserRoleAsync(user.Id, new UpdateUserRoleRequest("SuperAdmin"));

        await act.Should().ThrowAsync<InvalidRoleException>();
    }

    [Theory]
    [InlineData("user")]
    [InlineData("User")]
    [InlineData("USER")]
    [InlineData("admin")]
    [InlineData("Admin")]
    [InlineData("finance")]
    [InlineData("Finance")]
    public async Task UpdateUserRoleAsync_ShouldBeCaseInsensitive(string roleInput)
    {
        var user = User.Create("alice@example.com", "hash");
        _users.FindByIdAsync(user.Id, default).Returns(user);

        var act = () => _service.UpdateUserRoleAsync(user.Id, new UpdateUserRoleRequest(roleInput));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ShouldThrow_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, default).ReturnsNull();

        var act = () => _service.UpdateUserRoleAsync(userId, new UpdateUserRoleRequest("Admin"));

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    // ── FreezeWalletAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task FreezeWalletAsync_ShouldFreezeWallet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        _wallets.FindByIdAsync(wallet.Id, default).Returns(wallet);

        var result = await _service.FreezeWalletAsync(wallet.Id);

        result.IsActive.Should().BeFalse();
        await _wallets.Received(1).UpdateAsync(wallet, default);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task FreezeWalletAsync_ShouldBeIdempotent_WhenAlreadyFrozen()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Freeze();
        _wallets.FindByIdAsync(wallet.Id, default).Returns(wallet);

        var result = await _service.FreezeWalletAsync(wallet.Id);

        // No additional UpdateAsync or SaveChanges since already frozen
        result.IsActive.Should().BeFalse();
        await _wallets.DidNotReceive().UpdateAsync(Arg.Any<Wallet>(), default);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    [Fact]
    public async Task FreezeWalletAsync_ShouldThrow_WhenWalletNotFound()
    {
        var walletId = Guid.NewGuid();
        _wallets.FindByIdAsync(walletId, default).ReturnsNull();

        var act = () => _service.FreezeWalletAsync(walletId);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    // ── UnfreezeWalletAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UnfreezeWalletAsync_ShouldUnfreezeWallet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Freeze();
        _wallets.FindByIdAsync(wallet.Id, default).Returns(wallet);

        var result = await _service.UnfreezeWalletAsync(wallet.Id);

        result.IsActive.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task UnfreezeWalletAsync_ShouldBeIdempotent_WhenAlreadyActive()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        _wallets.FindByIdAsync(wallet.Id, default).Returns(wallet);

        var result = await _service.UnfreezeWalletAsync(wallet.Id);

        result.IsActive.Should().BeTrue();
        await _wallets.DidNotReceive().UpdateAsync(Arg.Any<Wallet>(), default);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }
}
