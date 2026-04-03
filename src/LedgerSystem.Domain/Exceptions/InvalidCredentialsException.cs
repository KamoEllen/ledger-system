namespace LedgerSystem.Domain.Exceptions;

/// <summary>
/// Returned on login failure. Intentionally generic — does not reveal
/// whether the email or password was incorrect (prevents user enumeration).
/// </summary>
public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("INVALID_CREDENTIALS", "Invalid email or password.")
    {
    }
}
