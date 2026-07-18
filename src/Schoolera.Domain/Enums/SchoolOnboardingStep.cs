namespace Schoolera.Domain.Enums;

/// <summary>
/// Multi-step progress marker for the onboarding wizard. Used to restore the
/// owner to the furthest step reached while a draft is still editable.
/// </summary>
public enum SchoolOnboardingStep
{
    Organization = 1,
    AuthorizedRepresentative = 2,
    SchoolDetails = 3,
    PrimaryBranch = 4,
    Documents = 5,
    Review = 6,
}
