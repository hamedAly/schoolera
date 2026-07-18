namespace Schoolera.Domain.Enums;

/// <summary>
/// Lifecycle status of a school-owner onboarding application.
/// Approved and Rejected are terminal states.
/// </summary>
public enum SchoolOnboardingStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    ChangesRequested = 4,
    Approved = 5,
    Rejected = 6,
}
