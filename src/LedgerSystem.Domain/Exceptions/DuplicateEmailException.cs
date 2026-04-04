namespace LedgerSystem.Domain.Exceptions;

public sealed class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string email)
        : base("EMAIL_ALREADY_EXISTS", $"A user with email '{email}' already exists.")
    {
    }
}
