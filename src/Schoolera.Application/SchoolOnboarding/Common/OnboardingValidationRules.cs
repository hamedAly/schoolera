using System.Text.RegularExpressions;

namespace Schoolera.Application.SchoolOnboarding.Common;

/// <summary>
/// Format predicates for draft-friendly step validators. Optional fields are only validated
/// when a value is supplied; required-ness is enforced at submit by <see cref="OnboardingCompleteness"/>.
/// </summary>
public static partial class OnboardingValidationRules
{
    public static bool BeAValidOptionalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static bool BeAValidOptionalEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) || EmailRegex().IsMatch(value);

    public static bool BeAValidOptionalPhone(string? value) =>
        string.IsNullOrWhiteSpace(value) || PhoneRegex().IsMatch(value);

    public static bool BeAValidOptionalCountryCode(string? value) =>
        string.IsNullOrWhiteSpace(value) || CountryCodeRegex().IsMatch(value);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?[0-9\s\-()]{7,20}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex("^[A-Za-z]{2}$")]
    private static partial Regex CountryCodeRegex();
}
