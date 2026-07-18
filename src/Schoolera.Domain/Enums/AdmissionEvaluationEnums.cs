namespace Schoolera.Domain.Enums;

public enum EvaluationTemplateKind
{
    Interview = 1,
    Assessment = 2,
    InterviewAndAssessment = 3,
}

public enum EvaluationTemplatePublicationStatus
{
    Draft = 1,
    Published = 2,
}

public enum EvaluationCriterionType
{
    YesNo = 1,
    RatingOneToFive = 2,
    SingleChoice = 3,
    ShortText = 4,
}

public enum EvaluationResultState
{
    Draft = 1,
    Finalized = 2,
}

public enum EvaluationAttendance
{
    Unknown = 1,
    Present = 2,
    Absent = 3,
    NotRequired = 4,
}

public enum EvaluationRecommendation
{
    NoRecommendation = 1,
    RecommendAccept = 2,
    RecommendReject = 3,
    RecommendWaitlist = 4,
    RescheduleRequired = 5,
    ManualReviewRequired = 6,
}

public enum AdmissionEvaluationHistoryAction
{
    SessionStarted = 1,
    DraftSaved = 2,
    ResultFinalized = 3,
    NoShowRecorded = 4,
    CorrectionStarted = 5,
    CorrectionFinalized = 6,
}
