namespace Schoolera.Application.SchoolOnboarding.Constants;

/// <summary>
/// Stable, machine-readable onboarding error codes. Angular branches on these codes,
/// never on localized <c>Errors</c> text.
/// </summary>
public static class OnboardingErrorCodes
{
    public const string NotFound = "onboarding.notFound";
    public const string Forbidden = "onboarding.forbidden";
    public const string OwnerRoleRequired = "onboarding.ownerRoleRequired";
    public const string InvalidStatusTransition = "onboarding.invalidStatusTransition";
    public const string NotEditable = "onboarding.notEditable";
    public const string ConcurrentUpdate = "onboarding.concurrentUpdate";
    public const string Incomplete = "onboarding.incomplete";
    public const string RequiredDocumentMissing = "onboarding.requiredDocumentMissing";
    public const string InvalidDocumentType = "onboarding.invalidDocumentType";
    public const string DocumentTooLarge = "onboarding.documentTooLarge";
    public const string UnsupportedDocumentFormat = "onboarding.unsupportedDocumentFormat";
    public const string InvalidFileSignature = "onboarding.invalidFileSignature";
    public const string EmptyDocument = "onboarding.emptyDocument";
    public const string CityDistrictMismatch = "onboarding.cityDistrictMismatch";
    public const string DuplicateRegistrationNumber = "onboarding.duplicateRegistrationNumber";
    public const string AlreadyApproved = "onboarding.alreadyApproved";
    public const string ApprovalFailed = "onboarding.approvalFailed";
    public const string ReasonRequired = "onboarding.reasonRequired";
}
