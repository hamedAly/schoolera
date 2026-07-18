namespace Schoolera.Application.Common.Models;

/// <summary>
/// Stable school error codes. Frontends branch on these, never on localized text.
/// </summary>
public static class SchoolErrorCodes
{
    public const string NotFound = "school.not_found";
    public const string SlugDuplicate = "school.slug_duplicate";
    public const string ContactConsentRequired = "school.contact.consent_required";
    public const string ContactInvalidSource = "school.contact.invalid_source";
    public const string ContactRejected = "school.contact.rejected";
}
