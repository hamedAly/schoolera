namespace Schoolera.Application.Common.Models;

/// <summary>
/// Stable machine-readable error codes. Display text remains in <see cref="Result{T}.Errors"/>.
/// Frontends must branch on codes, never on localized message text.
/// </summary>
public static class ErrorCodes
{
    public const string Unexpected = "error.unexpected";
    public const string Validation = "error.validation";
    public const string NotFound = "error.not_found";
    public const string Unauthorized = "error.unauthorized";
    public const string Forbidden = "error.forbidden";
}
