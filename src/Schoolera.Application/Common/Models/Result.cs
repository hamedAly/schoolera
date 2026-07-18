namespace Schoolera.Application.Common.Models;

public sealed record Result<T>
{
    private Result(
        bool succeeded,
        T? data,
        IReadOnlyCollection<string> errors,
        IReadOnlyCollection<string> errorCodes)
    {
        Succeeded = succeeded;
        Data = data;
        Errors = errors;
        ErrorCodes = errorCodes;
    }

    public bool Succeeded { get; }

    public T? Data { get; }

    /// <summary>Localized display messages for humans.</summary>
    public IReadOnlyCollection<string> Errors { get; }

    /// <summary>Stable codes for client logic. Never parse <see cref="Errors"/> for branching.</summary>
    public IReadOnlyCollection<string> ErrorCodes { get; }

    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data, Array.Empty<string>(), Array.Empty<string>());
    }

    public static Result<T> Failure(IEnumerable<string> errors, IEnumerable<string>? errorCodes = null)
    {
        return new Result<T>(
            false,
            default,
            errors.ToArray(),
            (errorCodes ?? Array.Empty<string>()).ToArray());
    }

    /// <summary>
    /// Failure that still carries structured payload (e.g. missing admission requirements).
    /// Clients must branch on <see cref="ErrorCodes"/>, not localized <see cref="Errors"/>.
    /// </summary>
    public static Result<T> Failure(
        T data,
        IEnumerable<string> errors,
        IEnumerable<string>? errorCodes = null)
    {
        return new Result<T>(
            false,
            data,
            errors.ToArray(),
            (errorCodes ?? Array.Empty<string>()).ToArray());
    }
}
