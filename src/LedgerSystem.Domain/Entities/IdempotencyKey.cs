namespace LedgerSystem.Domain.Entities;

/// <summary>
/// Stores the result of a mutating API request keyed by the client-supplied idempotency key.
/// If the same key is received again within the TTL, the stored response is returned
/// without re-executing the operation.
/// </summary>
public sealed class IdempotencyKey
{
    public string Key { get; private set; }
    public Guid UserId { get; private set; }
    public string RequestPath { get; private set; }
    public int ResponseStatus { get; private set; }

    /// <summary>
    /// Full serialised response body stored as JSON.
    /// Replayed verbatim on duplicate requests.
    /// </summary>
    public string ResponseBody { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Keys expire after 24 hours. After expiry the same key is treated as a new request.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    // Required by EF Core
    private IdempotencyKey()
    {
        Key = string.Empty;
        RequestPath = string.Empty;
        ResponseBody = string.Empty;
    }

    private IdempotencyKey(
        string key,
        Guid userId,
        string requestPath,
        int responseStatus,
        string responseBody,
        DateTime now)
    {
        Key = key;
        UserId = userId;
        RequestPath = requestPath;
        ResponseStatus = responseStatus;
        ResponseBody = responseBody;
        CreatedAt = now;
        ExpiresAt = now.AddHours(24);
    }

    public static IdempotencyKey Create(
        string key,
        Guid userId,
        string requestPath,
        int responseStatus,
        string responseBody)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Idempotency key cannot be empty.", nameof(key));

        if (string.IsNullOrWhiteSpace(requestPath))
            throw new ArgumentException("Request path cannot be empty.", nameof(requestPath));

        return new IdempotencyKey(key, userId, requestPath, responseStatus, responseBody, DateTime.UtcNow);
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
