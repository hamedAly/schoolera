namespace Schoolera.Domain.Enums;

public enum FaqOwnershipScope
{
    Platform = 1,
    School = 2,
}

/// <summary>
/// Null on FaqItem = general CMS FAQ (parents/schools/platform).
/// Non-null = Interview/Assessment FAQ.
/// </summary>
public enum InterviewFaqCategory
{
    Interview = 1,
    Assessment = 2,
    InterviewAndAssessment = 3,
}
