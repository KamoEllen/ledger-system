using LedgerSystem.Application.DTOs.Admin;
using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using LedgerSystem.Domain.Exceptions;

namespace LedgerSystem.Application.Services;

public sealed class AdminService : IAdminService
{
    private readonly IUserRepository _users;
    private readonly IWalletRepository _wallets;
    private readonly IUnitOfWork _uow;

    public AdminService(
        IUserRepository users,
        IWalletRepository wallets,
        IUnitOfWork uow)
    {
        _users = users;
        _wallets = wallets;
        _uow = uow;
    }

    public async Task<PagedResponse<AdminUserDto>> GetUsersAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var users = await _users.GetAllAsync(page, pageSize, ct);
        var total = await _users.CountAllAsync(ct);
        var items = users.Select(MapUser).ToList();
        return new PagedResponse<AdminUserDto>(items, page, pageSize, total);
    }

    public async Task<AdminUserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        return MapUser(user);
    }

    public async Task<AdminUserDto> UpdateUserRoleAsync(
        Guid userId, UpdateUserRoleRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var newRole))
            throw new InvalidRoleException(request.Role);

        user.UpdateRole(newRole);
        await _users.UpdateAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return MapUser(user);
    }

    public async Task<WalletDto> FreezeWalletAsync(Guid walletId, CancellationToken ct = default)
    {
        var wallet = await _wallets.FindByIdAsync(walletId, ct)
            ?? throw new WalletNotFoundException(walletId);

        // Idempotent: if already frozen, return as-is
        if (wallet.IsActive)
        {
            wallet.Freeze();
            await _wallets.UpdateAsync(wallet, ct);
            await _uow.SaveChangesAsync(ct);
        }

        return MapWallet(wallet);
    }

    public async Task<WalletDto> UnfreezeWalletAsync(Guid walletId, CancellationToken ct = default)
    {
        var wallet = await _wallets.FindByIdAsync(walletId, ct)
            ?? throw new WalletNotFoundException(walletId);

        // Idempotent: if already active, return as-is
        if (!wallet.IsActive)
        {
            wallet.Unfreeze();
            await _wallets.UpdateAsync(wallet, ct);
            await _uow.SaveChangesAsync(ct);
        }

        return MapWallet(wallet);
    }

    private static AdminUserDto MapUser(User u) =>
        new(u.Id, u.Email, u.Role.ToString(), u.CreatedAt);

    private static WalletDto MapWallet(Wallet w) =>
        new(w.Id, w.UserId, w.Currency, w.Balance, w.IsActive, w.CreatedAt);
}
