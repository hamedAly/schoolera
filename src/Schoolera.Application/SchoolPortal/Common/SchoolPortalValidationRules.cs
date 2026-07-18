using Schoolera.Application.SchoolOnboarding.Common;

namespace Schoolera.Application.SchoolPortal.Common;

/// <summary>Shared validation predicates for school portal commands.</summary>
public static class SchoolPortalValidationRules
{
    public static bool BeAValidOptionalUrl(string? value) =>
        OnboardingValidationRules.BeAValidOptionalUrl(value);

    public static bool BeAValidOptionalEmail(string? value) =>
        OnboardingValidationRules.BeAValidOptionalEmail(value);

    public static bool BeAValidOptionalPhone(string? value) =>
        OnboardingValidationRules.BeAValidOptionalPhone(value);

    public static bool BeAValidFoundedYear(int? value) =>
        !value.HasValue || (value >= 1800 && value <= DateTime.UtcNow.Year);
}
