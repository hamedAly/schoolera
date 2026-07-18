namespace Schoolera.Domain.Enums;

public enum InterviewAssessmentRequirementMode
{
    NotRequired = 1,
    InterviewOnly = 2,
    AssessmentOnly = 3,
    InterviewAndAssessment = 4,
}

public enum InterviewAssessmentDeliveryMode
{
    Online = 1,
    OnSite = 2,
    Hybrid = 3,
}

public enum InterviewAssessmentRequiredParticipants
{
    Child = 1,
    ParentOrGuardian = 2,
    Both = 3,
}

public enum HybridDeliverySelectionAuthority
{
    SchoolSelects = 1,
    ParentMayChoose = 2,
}

public enum InterviewAssessmentPolicyPublicationStatus
{
    Draft = 1,
    Published = 2,
}
