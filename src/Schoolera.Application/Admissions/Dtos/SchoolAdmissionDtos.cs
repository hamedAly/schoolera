using Schoolera.Application.Admissions.Common;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Dtos;

public sealed record SchoolAdmissionApplicationListQuery(
    AdmissionApplicationStatus? Status,
    Guid? BranchId,
    Guid? GradeId,
    Guid? EducationalStageId,
    Guid? AcademicYearId,
    string? Search,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string Sort,
    int PageNumber,
    int PageSize,
    IReadOnlyList<Guid>? RestrictToBranchIds = null);

public sealed record SchoolAdmissionReviewCapabilitiesDto(
    bool CanStartReview,
    bool CanRequestMissingItems,
    bool CanScheduleInterview,
    bool CanScheduleAssessment,
    bool CanMoveToWaitingList,
    bool CanAccept,
    bool CanReject,
    bool CanMarkRegistered,
    bool CanRescheduleInterview,
    bool CanRescheduleAssessment,
    bool CanCancelInterviewAppointment,
    bool CanCancelAssessmentAppointment,
    bool CanCompleteInterview,
    bool CanCompleteAssessment,
    bool CanReturnToUnderReview,
    bool CanDownloadAttachments,
    bool CanGrantAgeException);

public sealed record SchoolAdmissionApplicationListItemDto(
    Guid Id,
    string ApplicationNumber,
    AdmissionApplicationStatus Status,
    string StudentName,
    string ParentName,
    string SchoolName,
    string BranchName,
    string StageName,
    string GradeName,
    string AcademicYearName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewStartedAtUtc,
    SchoolAdmissionReviewCapabilitiesDto Capabilities);

public sealed record SchoolAdmissionApplicationDetailDto(
    Guid Id,
    string ApplicationNumber,
    AdmissionApplicationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewStartedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RejectedAtUtc,
    string StudentName,
    string? StudentMaskedIdentity,
    DateOnly? StudentBirthDate,
    ChildGender? StudentGender,
    string? StudentCurrentGradeName,
    string? StudentCurrentSchoolName,
    ChildStudyLanguage? StudentPreferredStudyLanguage,
    string? StudentSkills,
    string? StudentHobbies,
    string? StudentStrengths,
    string? StudentImprovementAreas,
    bool StudentHasSpecialNeeds,
    string? StudentSpecialNeedsNotes,
    string ParentName,
    string? ParentEmail,
    string? ParentPhone,
    string? ParentAlternatePhone,
    string? FatherFullName,
    string? FatherPhone,
    string? FatherEmail,
    string? FatherOccupation,
    string? FatherQualification,
    string? FatherMaskedIdentity,
    string? MotherFullName,
    string? MotherPhone,
    string? MotherEmail,
    string? MotherOccupation,
    string? MotherQualification,
    string? MotherMaskedIdentity,
    Guid SchoolId,
    string SchoolName,
    Guid SchoolBranchId,
    string BranchName,
    Guid EducationalStageId,
    string StageName,
    Guid GradeId,
    string GradeName,
    Guid AcademicYearId,
    string AcademicYearName,
    string? ParentNotes,
    string? SchoolNotes,
    string? RejectionReason,
    IReadOnlyList<AdmissionAttachmentDto> Attachments,
    IReadOnlyList<SchoolAdmissionHistoryDto> Timeline,
    IReadOnlyList<AdmissionRequirementChecklistItemDto> Requirements,
    IReadOnlyList<AdmissionQuestionChecklistItemDto> Questions,
    AdmissionMissingItemsRequestDto? ActiveMissingItemsRequest,
    AdmissionAppointmentDto? ActiveInterview,
    AdmissionAppointmentDto? ActiveAssessment,
    AdmissionWaitingListDto? WaitingList,
    DateTimeOffset? RegisteredAtUtc,
    SafeInterviewAssessmentPolicySummaryDto? PolicySummary,
    AgeEligibilityResultDto? AgeEligibility,
    SchoolAdmissionReviewCapabilitiesDto Capabilities,
    byte[] RowVersion);

public sealed record SchoolAdmissionHistoryDto(
    Guid Id,
    AdmissionApplicationStatus? FromStatus,
    AdmissionApplicationStatus ToStatus,
    string Action,
    string? InternalNote,
    string? ParentVisibleNote,
    Guid ActorUserId,
    string ActorRole,
    DateTimeOffset CreatedAtUtc);

public sealed record RejectSchoolAdmissionApplicationRequest(
    string ParentVisibleRejectionReason,
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record AcceptSchoolAdmissionApplicationRequest(
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record StartSchoolAdmissionReviewRequest(
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record SchoolAdmissionRecentItemDto(
    Guid Id,
    string ApplicationNumber,
    string StudentName,
    AdmissionApplicationStatus Status,
    DateTimeOffset? SubmittedAtUtc);

public sealed record SchoolAdmissionDashboardCountsDto(
    int Submitted,
    int UnderReview,
    int MissingDocuments,
    int InterviewRequired,
    int AssessmentRequired,
    int WaitingList,
    int Accepted,
    int Rejected,
    int Registered,
    int TotalActive,
    IReadOnlyList<SchoolAdmissionRecentItemDto> RecentSubmitted);

public static class SchoolAdmissionCapabilityFactory
{
    public static SchoolAdmissionReviewCapabilitiesDto From(
        AdmissionApplicationStatus status,
        bool canGrantAgeException = false) =>
        new(
            CanStartReview: AdmissionTransitionPolicy.CanSchoolStartReview(status),
            CanRequestMissingItems: AdmissionTransitionPolicy.CanSchoolRequestMissingItems(status),
            CanScheduleInterview: AdmissionTransitionPolicy.CanSchoolScheduleInterview(status),
            CanScheduleAssessment: AdmissionTransitionPolicy.CanSchoolScheduleAssessment(status),
            CanMoveToWaitingList: AdmissionTransitionPolicy.CanSchoolMoveToWaitingList(status),
            CanAccept: AdmissionTransitionPolicy.CanSchoolAccept(status),
            CanReject: AdmissionTransitionPolicy.CanSchoolReject(status),
            CanMarkRegistered: AdmissionTransitionPolicy.CanSchoolMarkRegistered(status),
            CanRescheduleInterview: AdmissionTransitionPolicy.CanSchoolRescheduleInterview(status),
            CanRescheduleAssessment: AdmissionTransitionPolicy.CanSchoolRescheduleAssessment(status),
            CanCancelInterviewAppointment: AdmissionTransitionPolicy.CanSchoolCancelInterviewAppointment(status),
            CanCancelAssessmentAppointment: AdmissionTransitionPolicy.CanSchoolCancelAssessmentAppointment(status),
            CanCompleteInterview: false,
            CanCompleteAssessment: false,
            CanReturnToUnderReview: AdmissionTransitionPolicy.CanSchoolReturnToUnderReview(status),
            CanDownloadAttachments: status is not AdmissionApplicationStatus.Draft,
            CanGrantAgeException: canGrantAgeException);
}
