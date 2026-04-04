using LedgerSystem.Application;
using LedgerSystem.Application.DTOs.Admin;
using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSystem.API.Controllers;

/// <summary>
/// Administrative endpoints. All actions require the Admin role.
///
/// Capabilities:
///   • List / get users
///   • Update user role (promote/demote)
///   • Freeze / unfreeze any wallet (no ownership check)
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.RequireAdmin)]
[Produces("application/json")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    /// <summary>Lists all registered users (paginated).</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResponse<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await _adminService.GetUsersAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Returns a single user by ID.</summary>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var result = await _adminService.GetUserByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Promotes or demotes a user to a new role.</summary>
    [HttpPatch("users/{id:guid}/role")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken ct)
    {
        var result = await _adminService.UpdateUserRoleAsync(id, request, ct);
        return Ok(result);
    }

    // ── Wallets ───────────────────────────────────────────────────────────────

    /// <summary>Freezes a wallet. Idempotent — returns 200 even if already frozen.</summary>
    [HttpPost("wallets/{id:guid}/freeze")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FreezeWallet(Guid id, CancellationToken ct)
    {
        var result = await _adminService.FreezeWalletAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Unfreezes a wallet. Idempotent — returns 200 even if already active.</summary>
    [HttpPost("wallets/{id:guid}/unfreeze")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfreezeWallet(Guid id, CancellationToken ct)
    {
        var result = await _adminService.UnfreezeWalletAsync(id, ct);
        return Ok(result);
    }
}
