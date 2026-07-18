using System.Text.RegularExpressions;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Common;

/// <summary>
/// Aggregate completeness rules shared by the read model (section flags) and the
/// submit/resubmit handlers (full-validation error codes). Draft saves are exempt.
/// </summary>
public static partial class OnboardingCompleteness
{
    public static bool IsOrganizationComplete(SchoolOnboardingApplication a) =>
        !string.IsNullOrWhiteSpace(a.OrganizationNameAr) &&
        !string.IsNullOrWhiteSpace(a.CountryCode) &&
        !string.IsNullOrWhiteSpace(a.RegistrationOrLicenseNumber);

    public static bool IsRepresentativeComplete(SchoolOnboardingApplication a) =>
        !string.IsNullOrWhiteSpace(a.RepresentativeFullNameAr) &&
        !string.IsNullOrWhiteSpace(a.RepresentativeNationalOrIdentityReference) &&
        !string.IsNullOrWhiteSpace(a.RepresentativeJobTitleAr) &&
        IsValidEmail(a.RepresentativeEmail) &&
        !string.IsNullOrWhiteSpace(a.RepresentativePhone);

    public static bool IsSchoolComplete(SchoolOnboardingApplication a) =>
        !string.IsNullOrWhiteSpace(a.SchoolNameAr) &&
        a.SchoolType is not null &&
        a.GenderType is not null;

    public static bool IsBranchComplete(SchoolOnboardingApplication a) =>
        a.CityId is not null &&
        a.DistrictId is not null &&
        !string.IsNullOrWhiteSpace(a.AddressLineAr) &&
        !string.IsNullOrWhiteSpace(a.PublicPhone);

    public static IReadOnlyList<Guid> MissingRequiredDocumentTypeIds(
        IEnumerable<Guid> requiredDocumentTypeIds,
        IEnumerable<Guid> currentDocumentTypeIds)
    {
        var present = currentDocumentTypeIds.ToHashSet();
        return requiredDocumentTypeIds.Where(id => !present.Contains(id)).ToArray();
    }

    /// <summary>Returns the distinct stable error codes that block submission, or empty when valid.</summary>
    public static IReadOnlyList<string> GetSubmissionErrorCodes(
        SchoolOnboardingApplication application,
        IReadOnlyList<Guid> requiredDocumentTypeIds,
        IReadOnlyList<Guid> currentDocumentTypeIds,
        bool districtBelongsToCity)
    {
        var codes = new List<string>();

        if (!IsOrganizationComplete(application) ||
            !IsRepresentativeComplete(application) ||
            !IsSchoolComplete(application) ||
            !IsBranchComplete(application))
        {
            codes.Add(OnboardingErrorCodes.Incomplete);
        }

        if (application.FoundedYear is { } year && (year < 1800 || year > DateTimeOffset.UtcNow.Year))
        {
            codes.Add(OnboardingErrorCodes.Incomplete);
        }

        if (application.Latitude is { } lat && (lat < -90m || lat > 90m))
        {
            codes.Add(OnboardingErrorCodes.Incomplete);
        }

        if (application.Longitude is { } lng && (lng < -180m || lng > 180m))
        {
            codes.Add(OnboardingErrorCodes.Incomplete);
        }

        if (application.CityId is not null && application.DistrictId is not null && !districtBelongsToCity)
        {
            codes.Add(OnboardingErrorCodes.CityDistrictMismatch);
        }

        if (MissingRequiredDocumentTypeIds(requiredDocumentTypeIds, currentDocumentTypeIds).Count > 0)
        {
            codes.Add(OnboardingErrorCodes.RequiredDocumentMissing);
        }

        return codes.Distinct().ToArray();
    }

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && EmailRegex().IsMatch(value);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
