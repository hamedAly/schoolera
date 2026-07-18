using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Couriers.Commands.MutateCourier;

public sealed class MutateCourierCommandValidator : AbstractValidator<MutateCourierCommand>
{
    public MutateCourierCommandValidator()
    {
        RuleFor(x => x.IntegrationId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x).Must(HasRequiredPayload).WithMessage("The courier operation payload is required.");
        RuleFor(x => x).Must(HasChildWhenRequired).WithMessage("The courier child identifier is required.");
        RuleFor(x => ExpectedRowVersion(x)).NotEmpty().Must(BeBase64);
        RuleFor(x => ExpectedConfigurationVersion(x)).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(HasValidWindow).WithMessage("Operating windows cannot overlap midnight.");
        RuleFor(x => x).Must(HasPositiveSla).WithMessage("SLA values must be positive.");
        RuleFor(x => x).Must(HasValidIdentifiers).WithMessage("Identifiers cannot be empty.");
        RuleFor(x => x).Must(HasValidProfile).WithMessage("The courier profile is invalid.");
        RuleFor(x => x).Must(HasValidService).WithMessage("The courier service is invalid.");
        RuleFor(x => x).Must(HasValidCoverage).WithMessage("The courier coverage is invalid.");
        RuleFor(x => x).Must(HasValidWindowMetadata).WithMessage("The courier window metadata is invalid.");
        RuleFor(x => x).Must(HasValidSlaMetadata).WithMessage("The courier SLA metadata is invalid.");
    }

    private static bool HasRequiredPayload(MutateCourierCommand x) => x.Kind switch
    {
        CourierMutationKind.UpdateProfile => x.Profile is not null,
        CourierMutationKind.CreateService or CourierMutationKind.UpdateService => x.Service is not null,
        CourierMutationKind.CreateCoverage or CourierMutationKind.UpdateCoverage => x.Coverage is not null,
        CourierMutationKind.CreateWindow or CourierMutationKind.UpdateWindow => x.Window is not null,
        CourierMutationKind.CreateSla or CourierMutationKind.UpdateSla => x.Sla is not null,
        _ => x.Active is not null,
    };
    private static bool HasChildWhenRequired(MutateCourierCommand x) =>
        x.Kind is CourierMutationKind.UpdateProfile or CourierMutationKind.CreateService
            or CourierMutationKind.CreateCoverage or CourierMutationKind.CreateWindow or CourierMutationKind.CreateSla ||
        x.ChildId is { } id && id != Guid.Empty;
    private static string ExpectedRowVersion(MutateCourierCommand x) =>
        x.Profile?.ExpectedRowVersion ?? x.Service?.ExpectedRowVersion ?? x.Coverage?.ExpectedRowVersion ??
        x.Window?.ExpectedRowVersion ?? x.Sla?.ExpectedRowVersion ?? x.Active?.ExpectedRowVersion ?? string.Empty;
    private static int ExpectedConfigurationVersion(MutateCourierCommand x) =>
        x.Profile?.ExpectedConfigurationVersion ?? x.Service?.ExpectedConfigurationVersion ??
        x.Coverage?.ExpectedConfigurationVersion ?? x.Window?.ExpectedConfigurationVersion ??
        x.Sla?.ExpectedConfigurationVersion ?? x.Active?.ExpectedConfigurationVersion ?? -1;
    private static bool HasValidWindow(MutateCourierCommand x) =>
        x.Window is null || x.Window.LocalEndTime > x.Window.LocalStartTime;
    private static bool HasPositiveSla(MutateCourierCommand x) =>
        x.Sla is null || x.Sla.AcceptanceTargetMinutes > 0 && x.Sla.PickupSchedulingTargetMinutes > 0 &&
        x.Sla.PickupCompletionTargetMinutes > 0 && x.Sla.DeliveryToSchoolTargetMinutes > 0 &&
        x.Sla.ReceiptConfirmationTargetMinutes is null or > 0;
    private static bool HasValidIdentifiers(MutateCourierCommand x) =>
        x.ChildId != Guid.Empty &&
        x.Coverage?.ServiceId != Guid.Empty &&
        x.Window?.ServiceId != Guid.Empty && x.Window?.CoverageRuleId != Guid.Empty &&
        x.Sla?.ServiceId != Guid.Empty && x.Sla?.CoverageRuleId != Guid.Empty;
    private static bool HasValidProfile(MutateCourierCommand x) =>
        x.Profile is null ||
        BoundedRequired(x.Profile.DescriptionAr, FieldLengthLimits.CourierDescription) &&
        BoundedRequired(x.Profile.DescriptionEn, FieldLengthLimits.CourierDescription) &&
        BoundedOptional(x.Profile.LogoReference, FieldLengthLimits.CourierLogoReference) &&
        BoundedRequired(x.Profile.TermsUrl, FieldLengthLimits.Url) &&
        BoundedRequired(x.Profile.PrivacyUrl, FieldLengthLimits.Url) &&
        SafePublicUrl(x.Profile.TermsUrl) && SafePublicUrl(x.Profile.PrivacyUrl) &&
        (x.Profile.LogoReference is null || SafeRelativePath(x.Profile.LogoReference));
    private static bool HasValidService(MutateCourierCommand x) =>
        x.Service is null ||
        BoundedRequired(x.Service.Code, FieldLengthLimits.CourierCode) &&
        BoundedRequired(x.Service.NameAr, FieldLengthLimits.ServiceName) &&
        BoundedRequired(x.Service.NameEn, FieldLengthLimits.ServiceName) &&
        BoundedRequired(x.Service.DescriptionAr, FieldLengthLimits.CourierDescription) &&
        BoundedRequired(x.Service.DescriptionEn, FieldLengthLimits.CourierDescription) &&
        x.Service.SortOrder is >= 0 and <= 10000 &&
        x.Service.MinimumPickupLeadTimeMinutes is >= 1 and <= 10080 &&
        x.Service.MaximumFuturePickupDays is >= 1 and <= 365 &&
        x.Service.AcceptanceWindowMinutes is >= 1 and <= 1440 &&
        x.Service.MaximumEnvelopeWeightGrams is >= 1 and <= 100000 &&
        x.Service.MaximumEnvelopeLengthCm is > 0 and <= 1000 &&
        x.Service.MaximumEnvelopeWidthCm is > 0 and <= 1000 &&
        x.Service.MaximumEnvelopeHeightCm is > 0 and <= 1000 &&
        !x.Service.SupportsDropOffPoint;
    private static bool HasValidCoverage(MutateCourierCommand x) =>
        x.Coverage is null ||
        x.Coverage.CountryId != Guid.Empty &&
        (!x.Coverage.CityId.HasValue || x.Coverage.GovernorateId.HasValue) &&
        (!x.Coverage.DistrictId.HasValue || x.Coverage.CityId.HasValue) &&
        Enum.IsDefined(x.Coverage.Result) &&
        BoundedOptional(x.Coverage.NotesAr, FieldLengthLimits.Notes) &&
        BoundedOptional(x.Coverage.NotesEn, FieldLengthLimits.Notes);
    private static bool HasValidWindowMetadata(MutateCourierCommand x) =>
        x.Window is null ||
        (x.Window.CoverageRuleId.HasValue
            ? BoundedOptional(x.Window.ScopeKey, FieldLengthLimits.CourierScopeKey)
            : BoundedRequired(x.Window.ScopeKey, FieldLengthLimits.CourierScopeKey)) &&
        BoundedRequired(x.Window.TimeZoneId, FieldLengthLimits.CourierTimeZone) &&
        Enum.IsDefined(x.Window.DayOfWeek) &&
        ValidTimeZone(x.Window.TimeZoneId);
    private static bool HasValidSlaMetadata(MutateCourierCommand x) =>
        x.Sla is null ||
        (x.Sla.CoverageRuleId.HasValue
            ? BoundedOptional(x.Sla.ScopeKey, FieldLengthLimits.CourierScopeKey)
            : BoundedRequired(x.Sla.ScopeKey, FieldLengthLimits.CourierScopeKey));
    private static bool BoundedRequired(string? value, int maximum) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximum;
    private static bool BoundedOptional(string? value, int maximum) =>
        value is null || value.Trim().Length <= maximum;
    private static bool SafePublicUrl(string value)
    {
        if (SafeRelativePath(value)) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps &&
               !string.IsNullOrWhiteSpace(uri.Host) &&
               string.IsNullOrEmpty(uri.UserInfo) &&
               string.IsNullOrEmpty(uri.Fragment) &&
               !ContainsTraversal(value);
    }
    private static bool SafeRelativePath(string value) =>
        value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) &&
        !value.Contains('\\') &&
        !value.Contains('#') &&
        !ContainsTraversal(value);
    private static bool ContainsTraversal(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value)
                .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment is "." or "..");
        }
        catch (UriFormatException)
        {
            return true;
        }
    }
    private static bool ValidTimeZone(string value)
    {
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(value); return true; }
        catch { return false; }
    }
    private static bool BeBase64(string value)
    {
        try { return Convert.FromBase64String(value).Length > 0; }
        catch { return false; }
    }
}
