using LedgerSystem.Application.DTOs.Admin;
using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Wallets;

namespace LedgerSystem.Application.Interfaces.Services;

public interface IAdminService
{
    /// <summary>Returns a paginated list of all users in the system.</summary>
    Task<PagedResponse<AdminUserDto>> GetUsersAsync(
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Returns a single user by ID.</summary>
    Task<AdminUserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Updates the role of an existing user.</summary>
    Task<AdminUserDto> UpdateUserRoleAsync(
        Guid userId, UpdateUserRoleRequest request, CancellationToken ct = default);

    /// <summary>Freezes a wallet — no ownership check (admin bypass).</summary>
    Task<WalletDto> FreezeWalletAsync(Guid walletId, CancellationToken ct = default);

    /// <summary>Unfreezes a wallet — no ownership check (admin bypass).</summary>
    Task<WalletDto> UnfreezeWalletAsync(Guid walletId, CancellationToken ct = default);
}
