using LedgerSystem.API.Controllers;
using LedgerSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LedgerSystem.API.Middleware;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception: {Code}", ex.ErrorCode);
            await WriteProblemAsync(context, MapToStatusCode(ex), ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse(code, message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private static int MapToStatusCode(DomainException ex) => ex switch
    {
        InsufficientFundsException        => StatusCodes.Status422UnprocessableEntity,
        WalletFrozenException             => StatusCodes.Status422UnprocessableEntity,
        CurrencyMismatchException         => StatusCodes.Status422UnprocessableEntity,
        SelfTransferException             => StatusCodes.Status422UnprocessableEntity,
        WalletAlreadyExistsException      => StatusCodes.Status409Conflict,
        DuplicateEmailException           => StatusCodes.Status409Conflict,
        DuplicateTransferException        => StatusCodes.Status409Conflict,
        InvalidRoleException              => StatusCodes.Status400BadRequest,
        WalletNotFoundException           => StatusCodes.Status404NotFound,
        TransferNotFoundException         => StatusCodes.Status404NotFound,
        UserNotFoundException             => StatusCodes.Status404NotFound,
        InvalidCredentialsException       => StatusCodes.Status401Unauthorized,
        InvalidRefreshTokenException      => StatusCodes.Status401Unauthorized,
        UnauthorizedWalletAccessException => StatusCodes.Status403Forbidden,
        UnauthorizedTransferException     => StatusCodes.Status403Forbidden,
        _                                 => StatusCodes.Status400BadRequest
    };
}
