namespace Schoolera.Application.Common.Models;

public sealed record Result<T>
{
    private Result(bool succeeded, T? data, IReadOnlyCollection<string> errors)
    {
        Succeeded = succeeded;
        Data = data;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public T? Data { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data, Array.Empty<string>());
    }

    public static Result<T> Failure(IEnumerable<string> errors)
    {
        return new Result<T>(false, default, errors.ToArray());
    }
}