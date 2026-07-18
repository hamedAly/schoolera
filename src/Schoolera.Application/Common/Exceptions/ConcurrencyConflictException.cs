namespace Schoolera.Application.Common.Exceptions;

/// <summary>
/// Thrown by the persistence layer when an optimistic-concurrency token check fails,
/// so application handlers and the API can translate it into a stable error code.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ConcurrencyConflictException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
