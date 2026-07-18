namespace Schoolera.Domain.Enums;

/// <summary>
/// Parent/school admission application lifecycle statuses for Phase 1.
/// ContractSent and Paid are intentionally omitted; the central transition policy rejects unknown targets.
/// </summary>
public enum AdmissionApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Accepted = 4,
    Rejected = 5,
    Cancelled = 6,
    MissingDocuments = 7,
    InterviewRequired = 8,
    AssessmentRequired = 9,
    WaitingList = 10,
    Registered = 11,
}

/// <summary>Allowlisted parent attachment kinds (optional generic, or mapped from required document codes).</summary>
public enum AdmissionAttachmentType
{
    SupportingDocument = 1,
    BirthCertificate = 2,
    PreviousSchoolReport = 3,
    ChildPhoto = 4,
    MedicalReport = 5,
    Other = 99,
}

/// <summary>Kind of item requested during a MissingDocuments cycle (application-owned snapshots/fields only).</summary>
public enum AdmissionMissingItemKind
{
    RequirementSnapshot = 1,
    QuestionSnapshot = 2,
    ParentSnapshotField = 3,
    ChildSnapshotField = 4,
}

/// <summary>Approved parent/guardian snapshot field codes for missing-item requests.</summary>
public enum AdmissionParentSnapshotFieldCode
{
    DisplayName = 1,
    Phone = 2,
    AlternatePhone = 3,
    Email = 4,
    FatherFullName = 10,
    FatherPhone = 11,
    FatherEmail = 12,
    FatherOccupation = 13,
    FatherQualification = 14,
    MotherFullName = 20,
    MotherPhone = 21,
    MotherEmail = 22,
    MotherOccupation = 23,
    MotherQualification = 24,
}

/// <summary>Approved child snapshot field codes for missing-item requests (never HealthNotes).</summary>
public enum AdmissionChildSnapshotFieldCode
{
    FullName = 1,
    CurrentSchoolName = 2,
    PreferredStudyLanguage = 3,
    Skills = 4,
    Hobbies = 5,
    Strengths = 6,
    ImprovementAreas = 7,
    HasSpecialNeeds = 8,
    SpecialNeedsNotes = 9,
}

/// <summary>Interview or assessment attendance mode.</summary>
public enum AdmissionAppointmentMode
{
    Online = 1,
    InPerson = 2,
}

/// <summary>Lifecycle of a single interview/assessment appointment (not the application status).</summary>
public enum AdmissionAppointmentLifecycle
{
    Proposed = 1,
    Completed = 2,
    Cancelled = 3,
    RescheduleRequested = 4,
    Confirmed = 5,
    NoShow = 6,
    InProgress = 7,
}

public enum AppointmentRescheduleInitiator
{
    Parent = 1,
    School = 2,
}

public enum AdmissionAppointmentAction
{
    Proposed = 1,
    ProposalReplaced = 2,
    Confirmed = 3,
    AlternateSlotSelected = 4,
    RescheduleRequested = 5,
    RescheduleRequiredBySchool = 6,
    Cancelled = 7,
    Completed = 8,
    NoShow = 9,
    SessionStarted = 10,
}

public enum AdmissionAppointmentActorType
{
    Parent = 1,
    School = 2,
    System = 3,
}

/// <summary>Stable action names recorded on admission history rows.</summary>
public static class AdmissionHistoryActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Submitted = "Submitted";
    public const string Cancelled = "Cancelled";
    public const string AttachmentUploaded = "AttachmentUploaded";
    public const string AttachmentRemoved = "AttachmentRemoved";
    public const string RequirementsSnapshotCreated = "RequirementsSnapshotCreated";
    public const string RequirementsValidationFailed = "RequirementsValidationFailed";
    public const string RequirementsCompleted = "RequirementsCompleted";
    public const string QuestionsSnapshotCreated = "QuestionsSnapshotCreated";
    public const string QuestionsSnapshotReset = "QuestionsSnapshotReset";
    public const string QuestionsValidationFailed = "QuestionsValidationFailed";
    public const string QuestionsCompleted = "QuestionsCompleted";
    public const string PolicySnapshotCreated = "PolicySnapshotCreated";
    public const string PolicySnapshotReplaced = "PolicySnapshotReplaced";
    public const string PolicySnapshotScopeChangeRejected = "PolicySnapshotScopeChangeRejected";
    public const string AgeEligibilitySnapshotCreated = "AgeEligibilitySnapshotCreated";
    public const string AgeEligibilitySnapshotReplaced = "AgeEligibilitySnapshotReplaced";
    public const string AgeEligibilityRecalculated = "AgeEligibilityRecalculated";
    public const string AgeEligibilityExceptionGranted = "AgeEligibilityExceptionGranted";
    public const string AgeEligibilityExceptionInvalidated = "AgeEligibilityExceptionInvalidated";
    public const string ReviewStarted = "ReviewStarted";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string MissingDocumentsRequested = "MissingDocumentsRequested";
    public const string MissingDocumentsResubmitted = "MissingDocumentsResubmitted";
    public const string MissingItemCorrected = "MissingItemCorrected";
    public const string InterviewScheduled = "InterviewScheduled";
    public const string InterviewRescheduled = "InterviewRescheduled";
    public const string InterviewCancelled = "InterviewCancelled";
    public const string InterviewCompleted = "InterviewCompleted";
    public const string AssessmentScheduled = "AssessmentScheduled";
    public const string AssessmentRescheduled = "AssessmentRescheduled";
    public const string AssessmentCancelled = "AssessmentCancelled";
    public const string AssessmentCompleted = "AssessmentCompleted";
    public const string InterviewRescheduleRequired = "InterviewRescheduleRequired";
    public const string AssessmentRescheduleRequired = "AssessmentRescheduleRequired";
    public const string MovedToWaitingList = "MovedToWaitingList";
    public const string ReturnedFromWaitingList = "ReturnedFromWaitingList";
    public const string Registered = "Registered";
}
