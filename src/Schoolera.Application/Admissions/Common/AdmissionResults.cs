using System.Globalization;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Exceptions;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Constants;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Builds Parent Admission <see cref="Result{T}"/> failures and persistence conflict handling.</summary>
public static class AdmissionResults
{
    public static Result<T> Forbidden<T>(IStringLocalizer<AuthMessages> localizer) =>
        Result<T>.Failure([localizer["Forbidden"].Value], [AdmissionErrorCodes.Forbidden]);

    public static Result<T> Failure<T>(string message, string errorCode) =>
        Result<T>.Failure([message], [errorCode]);

    public static Result<T> NotFound<T>() =>
        Failure<T>("Admission application not found.", AdmissionErrorCodes.NotFound);

    public static Result<T> ReviewNotFound<T>() =>
        Failure<T>("Admission application not found.", AdmissionErrorCodes.ReviewNotFound);

    /// <summary>
    /// Remaps parent-style <see cref="AdmissionErrorCodes.ConcurrentUpdate"/> from
    /// <see cref="TrySaveAsync{T}"/> to school-review concurrency codes.
    /// </summary>
    public static Result<T> RemapSchoolSaveConflict<T>(Result<T> conflict)
    {
        if (conflict.ErrorCodes.Contains(AdmissionErrorCodes.ConcurrentUpdate))
        {
            return Failure<T>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ReviewConcurrentUpdate);
        }

        return conflict;
    }

    public static bool HasRowVersionMismatch(byte[]? requested, byte[] current) =>
        requested is { Length: > 0 } &&
        current.Length > 0 &&
        !requested.AsSpan().SequenceEqual(current);

    public static Result<T> MapPrivateFileError<T>(PrivateFileValidationException exception)
    {
        var code = exception.ErrorCode switch
        {
            OnboardingErrorCodes.DocumentTooLarge => AdmissionErrorCodes.AttachmentTooLarge,
            OnboardingErrorCodes.UnsupportedDocumentFormat => AdmissionErrorCodes.AttachmentTypeInvalid,
            OnboardingErrorCodes.InvalidFileSignature => AdmissionErrorCodes.AttachmentTypeInvalid,
            OnboardingErrorCodes.EmptyDocument => AdmissionErrorCodes.AttachmentTypeInvalid,
            OnboardingErrorCodes.InvalidDocumentType => AdmissionErrorCodes.AttachmentTypeInvalid,
            _ => AdmissionErrorCodes.AttachmentTypeInvalid,
        };

        var message = code switch
        {
            AdmissionErrorCodes.AttachmentTooLarge => "Attachment exceeds the maximum allowed size.",
            _ => "Attachment type or format is invalid.",
        };

        return Failure<T>(message, code);
    }

    /// <summary>
    /// Persists changes, mapping concurrency and unique-constraint conflicts to stable admission codes.
    /// Returns <c>null</c> when the save succeeds.
    /// </summary>
    public static async Task<Result<T>?> TrySaveAsync<T>(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConcurrencyConflictException)
        {
            return Failure<T>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ConcurrentUpdate);
        }
        catch (Exception exception) when (IsUniqueViolation(exception))
        {
            return Failure<T>(
                "An active admission application already exists for this selection.",
                AdmissionErrorCodes.DuplicateActiveApplication);
        }
        catch (Exception exception) when (IsConcurrencyViolation(exception))
        {
            return Failure<T>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ConcurrentUpdate);
        }
    }

    public static string PreferredLanguageCode()
    {
        var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return twoLetter.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().FullName ?? current.GetType().Name;
            if (typeName.Contains("DbUpdateException", StringComparison.Ordinal))
            {
                if (ContainsUniqueSqlNumber(current) ||
                    ContainsUniqueSqlNumber(current.InnerException) ||
                    MessageLooksLikeUnique(current.Message) ||
                    (current.InnerException is not null && MessageLooksLikeUnique(current.InnerException.Message)))
                {
                    return true;
                }
            }

            if (ContainsUniqueSqlNumber(current) || MessageLooksLikeUnique(current.Message))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsConcurrencyViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().FullName ?? current.GetType().Name;
            if (typeName.Contains("DbUpdateConcurrencyException", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsUniqueSqlNumber(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        var numberProperty = exception.GetType().GetProperty("Number");
        if (numberProperty?.GetValue(exception) is int number && number is 2601 or 2627)
        {
            return true;
        }

        return false;
    }

    private static bool MessageLooksLikeUnique(string? message) =>
        !string.IsNullOrWhiteSpace(message) &&
        (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("IX_AdmissionApplications_ActiveDuplicate", StringComparison.OrdinalIgnoreCase));
}
