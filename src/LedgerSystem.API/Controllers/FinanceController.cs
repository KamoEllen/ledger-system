using LedgerSystem.Application;
using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Transfers;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSystem.API.Controllers;

/// <summary>
/// Read-only financial reporting endpoints available to Finance and Admin roles.
///
/// Capabilities:
///   • List all transfers across the system (cross-user)
///   • Get any transfer by ID
///   • List all wallets across the system
/// </summary>
[ApiController]
[Route("api/finance")]
[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
[Produces("application/json")]
public sealed class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    // ── Transfers ─────────────────────────────────────────────────────────────

    /// <summary>Lists all transfers in the system (paginated, newest first).</summary>
    [HttpGet("transfers")]
    [ProducesResponseType(typeof(PagedResponse<TransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTransfers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await _financeService.GetAllTransfersAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Returns a single transfer by ID regardless of ownership.</summary>
    [HttpGet("transfers/{id:guid}")]
    [ProducesResponseType(typeof(TransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransfer(Guid id, CancellationToken ct)
    {
        var result = await _financeService.GetTransferByIdAsync(id, ct);
        return Ok(result);
    }

    // ── Wallets ───────────────────────────────────────────────────────────────

    /// <summary>Lists all wallets across all users (paginated, newest first).</summary>
    [HttpGet("wallets")]
    [ProducesResponseType(typeof(PagedResponse<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllWallets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await _financeService.GetAllWalletsAsync(page, pageSize, ct);
        return Ok(result);
    }
}
