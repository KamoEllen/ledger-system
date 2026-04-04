using LedgerSystem.API.Extensions;
using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Wallets;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSystem.API.Controllers;

[ApiController]
[Route("api/wallets")]
[Authorize]
[Produces("application/json")]
public sealed class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>Lists all wallets belonging to the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWallets(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var wallets = await _walletService.GetByUserIdAsync(userId, ct);
        return Ok(wallets);
    }

    /// <summary>Returns a single wallet with current balance.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WalletDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWallet(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var wallet = await _walletService.GetByIdAsync(id, userId, ct);
        return Ok(wallet);
    }

    /// <summary>Creates a new wallet for the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWallet(
        [FromBody] CreateWalletRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var wallet = await _walletService.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, wallet);
    }

    /// <summary>
    /// Returns the paginated ledger history for a wallet (most recent first).
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(PagedResponse<LedgerEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var userId = User.GetUserId();
        var history = await _walletService.GetHistoryAsync(id, userId, page, pageSize, ct);
        return Ok(history);
    }
}
