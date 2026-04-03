namespace LedgerSystem.Domain.Exceptions;

/// <summary>
/// Base class for all domain-level exceptions.
/// These represent business rule violations — not programming errors.
/// </summary>
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }

    protected DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
