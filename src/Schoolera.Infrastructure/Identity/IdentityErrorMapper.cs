using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Infrastructure.Identity;

/// <summary>
/// Maps ASP.NET Core Identity <see cref="IdentityError.Code"/> values to stable
/// Schoolera <c>auth.*</c> codes. Never branch clients on <see cref="IdentityError.Description"/>.
/// </summary>
public static class IdentityErrorMapper
{
    public static Result<T> ToFailureResult<T>(
        IEnumerable<IdentityError> identityErrors,
        IStringLocalizer<AuthMessages> authMessages)
    {
        var errors = identityErrors.ToArray();
        if (errors.Length == 0)
        {
            return Result<T>.Failure(
                [authMessages["ValidationFailed"].Value],
                [ErrorCodes.Validation]);
        }

        var codes = new List<string>(errors.Length);
        var messages = new List<string>(errors.Length);

        foreach (var error in errors)
        {
            var mapped = Map(error.Code);
            if (codes.Contains(mapped.ErrorCode, StringComparer.Ordinal))
            {
                continue;
            }

            codes.Add(mapped.ErrorCode);
            messages.Add(authMessages[mapped.ResourceKey].Value);
        }

        return Result<T>.Failure(messages, codes);
    }

    public static (string ErrorCode, string ResourceKey) Map(string? identityCode)
    {
        if (string.IsNullOrWhiteSpace(identityCode))
        {
            return (ErrorCodes.Validation, "ValidationFailed");
        }

        return identityCode switch
        {
            "PasswordRequiresNonAlphanumeric" => (
                AuthErrorCodes.PasswordRequiresNonAlphanumeric,
                "PasswordRequiresNonAlphanumeric"),
            "PasswordRequiresLower" => (
                AuthErrorCodes.PasswordRequiresLowercase,
                "PasswordRequiresLowercase"),
            "PasswordRequiresUpper" => (
                AuthErrorCodes.PasswordRequiresUppercase,
                "PasswordRequiresUppercase"),
            "PasswordRequiresDigit" => (
                AuthErrorCodes.PasswordRequiresDigit,
                "PasswordRequiresDigit"),
            "PasswordRequiresUniqueChars" => (
                AuthErrorCodes.PasswordRequiresUniqueChars,
                "PasswordRequiresUniqueChars"),
            "PasswordTooShort" => (
                AuthErrorCodes.PasswordTooShort,
                "PasswordTooShort"),
            "DuplicateEmail" or "DuplicateUserName" => (
                AuthErrorCodes.EmailAlreadyExists,
                "DuplicateEmail"),
            "InvalidEmail" => (
                AuthErrorCodes.InvalidEmail,
                "InvalidEmail"),
            "PasswordMismatch" => (
                AuthErrorCodes.InvalidResetToken,
                "InvalidResetToken"),
            "InvalidToken" => (
                AuthErrorCodes.InvalidResetToken,
                "InvalidResetToken"),
            _ => (ErrorCodes.Validation, "ValidationFailed"),
        };
    }
}
