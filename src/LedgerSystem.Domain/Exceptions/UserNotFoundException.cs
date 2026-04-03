namespace LedgerSystem.Domain.Exceptions;

public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId)
        : base("USER_NOT_FOUND", $"User '{userId}' was not found.")
    {
    }

    public UserNotFoundException(string email)
        : base("USER_NOT_FOUND", $"User with email '{email}' was not found.")
    {
    }
}
