using LedgerSystem.API.Extensions;
using LedgerSystem.Application.Interfaces.Services;
using System.Security.Claims;
using System.Text;

namespace LedgerSystem.API.Middleware;

/// <summary>
/// For POST/PATCH requests that include an Idempotency-Key header:
///   • If the key is already cached → replay the stored response without hitting the controller.
///   • Otherwise → let the request through, capture the response body, and cache it.
///
/// Only responses in the 2xx range are cached. Error responses are not cached so that
/// clients can retry after transient failures without providing a new key.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    private static readonly HashSet<string> _idempotentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PATCH" };

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        var request = context.Request;

        // Only intercept idempotency-key-bearing mutating requests.
        if (!_idempotentMethods.Contains(request.Method)
            || !request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        // Require an authenticated user so the cache is namespaced per user.
        var userId = TryGetUserId(context.User);
        if (userId is null)
        {
            await _next(context);
            return;
        }

        // ── Cache HIT → replay ─────────────────────────────────────────────
        var cached = await idempotencyService.GetCachedResponseAsync(idempotencyKey, userId.Value);
        if (cached is not null)
        {
            _logger.LogInformation(
                "Idempotency cache hit for key {Key}, replaying {Status}", idempotencyKey, cached.StatusCode);

            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers.Append("X-Idempotency-Replayed", "true");
            await context.Response.WriteAsync(cached.Body, Encoding.UTF8);
            return;
        }

        // ── Cache MISS → capture response ──────────────────────────────────
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        // Copy buffered response back to the real stream.
        buffer.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(originalBody);

        // Only cache successful (2xx) responses.
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            try
            {
                var cachedResponse = new CachedResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    responseBody);

                await idempotencyService.StoreResponseAsync(
                    idempotencyKey, userId.Value, request.Path.Value ?? string.Empty, cachedResponse);

                _logger.LogInformation(
                    "Cached idempotency response for key {Key}", idempotencyKey);
            }
            catch (Exception ex)
            {
                // Failure to cache must not fail the request.
                _logger.LogError(ex, "Failed to store idempotency cache for key {Key}", idempotencyKey);
            }
        }
    }

    private static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        try { return principal.GetUserId(); }
        catch { return null; }
    }
}
