namespace LedgerSystem.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// True when the token has not been revoked and has not yet expired.
    /// </summary>
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    // Required by EF Core
    private RefreshToken() { Token = string.Empty; }

    private RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt, DateTime now)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CreatedAt = now;
    }

    public static RefreshToken Create(Guid userId, string token, int expiryDays)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Refresh token cannot be empty.", nameof(token));

        if (expiryDays <= 0)
            throw new ArgumentException("Expiry days must be positive.", nameof(expiryDays));

        var now = DateTime.UtcNow;
        return new RefreshToken(Guid.NewGuid(), userId, token, now.AddDays(expiryDays), now);
    }

    /// <summary>
    /// Marks this token as revoked. Used during logout and refresh token rotation.
    /// Once revoked, a token can never be reactivated.
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked)
            throw new InvalidOperationException("Token is already revoked.");

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }
}
