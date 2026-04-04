namespace LedgerSystem.Domain.Exceptions;

public sealed class InvalidRoleException : DomainException
{
    public InvalidRoleException(string role)
        : base("INVALID_ROLE", $"'{role}' is not a valid user role. Valid values: User, Finance, Admin.")
    {
    }
}
