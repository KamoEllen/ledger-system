namespace LedgerSystem.Domain.Exceptions;

public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("INVALID_REFRESH_TOKEN", "The refresh token is invalid, expired, or has been revoked.")
    {
    }
}
