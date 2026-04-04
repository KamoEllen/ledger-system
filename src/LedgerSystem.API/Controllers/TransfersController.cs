using LedgerSystem.API.Extensions;
using LedgerSystem.Application.DTOs.Common;
using LedgerSystem.Application.DTOs.Transfers;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSystem.API.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
[Produces("application/json")]
public sealed class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// Initiates a double-entry transfer between two wallets.
    /// Requires a unique Idempotency-Key header — safe to retry on network failure.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransferResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTransfer(
        [FromBody] CreateTransferRequest request,
        CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ErrorResponse(
                "IDEMPOTENCY_KEY_REQUIRED",
                "The Idempotency-Key header is required for transfer requests."));
        }

        var serviceRequest = new TransferRequest(
            RequestingUserId: User.GetUserId(),
            SourceWalletId: request.SourceWalletId,
            DestinationWalletId: request.DestinationWalletId,
            Amount: request.Amount,
            Currency: request.Currency,
            IdempotencyKey: idempotencyKey,
            Description: request.Description);

        try
        {
            var result = await _transferService.ExecuteAsync(serviceRequest, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (DuplicateTransferException ex)
        {
            return Conflict(ErrorResponse.From(ex));
        }
        catch (WalletNotFoundException ex)
        {
            return NotFound(ErrorResponse.From(ex));
        }
        catch (UnauthorizedTransferException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.From(ex));
        }
        catch (InsufficientFundsException ex)
        {
            return UnprocessableEntity(ErrorResponse.From(ex));
        }
        catch (WalletFrozenException ex)
        {
            return UnprocessableEntity(ErrorResponse.From(ex));
        }
        catch (CurrencyMismatchException ex)
        {
            return UnprocessableEntity(ErrorResponse.From(ex));
        }
        catch (SelfTransferException ex)
        {
            return UnprocessableEntity(ErrorResponse.From(ex));
        }
    }

    /// <summary>Returns a single transfer visible to the authenticated user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransfer(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = User.GetUserId();
            var transfer = await _transferService.GetByIdAsync(id, userId, ct);
            return Ok(transfer);
        }
        catch (TransferNotFoundException ex)
        {
            return NotFound(ErrorResponse.From(ex));
        }
    }

    /// <summary>Lists all transfers across all wallets owned by the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransfers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var userId = User.GetUserId();
        var transfers = await _transferService.GetByUserAsync(userId, page, pageSize, ct);
        return Ok(transfers);
    }
}
