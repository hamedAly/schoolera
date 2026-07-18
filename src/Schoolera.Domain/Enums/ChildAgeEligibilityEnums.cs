namespace Schoolera.Domain.Enums;

public enum ChildAgeEligibilityPublicationStatus
{
    Draft = 1,
    Published = 2,
}

public enum ChildAgeReferenceDateMode
{
    /// <summary>Use <c>AcademicYear.StartDate</c> as the age reference.</summary>
    AcademicYearStart = 1,
}

public enum ChildAgeEligibilityResultCode
{
    Eligible = 1,
    NotEligibleBelowMinimum = 2,
    NotEligibleAboveMaximum = 3,
    BirthDateRequired = 4,
    /// <summary>Birth date is after the reference date.</summary>
    InvalidBirthDate = 5,
    RuleNotConfigured = 6,
    ManualExceptionApproved = 7,
}

public enum ChildAgeEligibilityExceptionReasonCode
{
    SchoolReviewApproved = 1,
    DocumentationReviewed = 2,
    OtherApproved = 99,
}
