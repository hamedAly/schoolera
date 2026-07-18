namespace Schoolera.Application.Legal.Constants;

/// <summary>Stable legal consent error codes. Frontends branch on these, never on localized text.</summary>
public static class LegalErrorCodes
{
    public const string TermsRequired = "legal.terms_required";
    public const string PrivacyRequired = "legal.privacy_required";
    public const string CurrentVersionMissing = "legal.current_version_missing";
}
