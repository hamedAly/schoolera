using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Exceptions;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Constants;

namespace Schoolera.Application.SchoolOnboarding.Common;

/// <summary>Builds localized <see cref="Result{T}"/> failures paired with stable onboarding codes.</summary>
public static class OnboardingResults
{
    private static readonly IReadOnlyDictionary<string, string> CodeToMessageKey = new Dictionary<string, string>
    {
        [OnboardingErrorCodes.NotFound] = "NotFound",
        [OnboardingErrorCodes.Forbidden] = "Forbidden",
        [OnboardingErrorCodes.OwnerRoleRequired] = "OwnerRoleRequired",
        [OnboardingErrorCodes.InvalidStatusTransition] = "InvalidStatusTransition",
        [OnboardingErrorCodes.NotEditable] = "NotEditable",
        [OnboardingErrorCodes.ConcurrentUpdate] = "ConcurrentUpdate",
        [OnboardingErrorCodes.Incomplete] = "Incomplete",
        [OnboardingErrorCodes.RequiredDocumentMissing] = "RequiredDocumentMissing",
        [OnboardingErrorCodes.InvalidDocumentType] = "InvalidDocumentType",
        [OnboardingErrorCodes.DocumentTooLarge] = "DocumentTooLarge",
        [OnboardingErrorCodes.UnsupportedDocumentFormat] = "UnsupportedDocumentFormat",
        [OnboardingErrorCodes.InvalidFileSignature] = "InvalidFileSignature",
        [OnboardingErrorCodes.EmptyDocument] = "EmptyDocument",
        [OnboardingErrorCodes.CityDistrictMismatch] = "CityDistrictMismatch",
        [OnboardingErrorCodes.DuplicateRegistrationNumber] = "DuplicateRegistrationNumber",
        [OnboardingErrorCodes.AlreadyApproved] = "AlreadyApproved",
        [OnboardingErrorCodes.ApprovalFailed] = "ApprovalFailed",
        [OnboardingErrorCodes.ReasonRequired] = "ReasonRequired",
    };

    public static Result<T> Failure<T>(
        IStringLocalizer<OnboardingMessages> localizer,
        string messageKey,
        string errorCode) =>
        Result<T>.Failure([localizer[messageKey].Value], [errorCode]);

    /// <summary>Builds a failure for a single stable code, resolving its localized message.</summary>
    public static Result<T> FailureForCode<T>(
        IStringLocalizer<OnboardingMessages> localizer,
        string errorCode) =>
        Result<T>.Failure([localizer[MessageKeyFor(errorCode)].Value], [errorCode]);

    /// <summary>Builds a failure carrying multiple stable codes with localized messages.</summary>
    public static Result<T> FailureForCodes<T>(
        IStringLocalizer<OnboardingMessages> localizer,
        IReadOnlyList<string> errorCodes)
    {
        var errors = errorCodes.Select(code => localizer[MessageKeyFor(code)].Value).ToArray();
        return Result<T>.Failure(errors, errorCodes);
    }

    /// <summary>
    /// Persists changes, translating an optimistic-concurrency conflict into a stable
    /// onboarding <c>ConcurrentUpdate</c> failure. Returns <c>null</c> when the save succeeds.
    /// </summary>
    public static async Task<Result<T>?> TrySaveAsync<T>(
        IUnitOfWork unitOfWork,
        IStringLocalizer<OnboardingMessages> localizer,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConcurrencyConflictException)
        {
            return Failure<T>(localizer, "ConcurrentUpdate", OnboardingErrorCodes.ConcurrentUpdate);
        }
    }

    private static string MessageKeyFor(string errorCode) =>
        CodeToMessageKey.TryGetValue(errorCode, out var key) ? key : "Incomplete";
}
