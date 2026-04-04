namespace LedgerSystem.Application.Interfaces.Services;

public record CachedResponse(int StatusCode, string ContentType, string Body);

public interface IIdempotencyService
{
    /// <summary>Returns a cached response for the given key + user, or null if not found / expired.</summary>
    Task<CachedResponse?> GetCachedResponseAsync(string key, Guid userId, CancellationToken ct = default);

    /// <summary>Stores the response body so it can be replayed on duplicate requests.</summary>
    Task StoreResponseAsync(
        string key, Guid userId, string requestPath, CachedResponse response, CancellationToken ct = default);
}
