using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Domain.Enums;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.Admissions.Dtos;

public sealed record CreateAdmissionApplicationRequest(
    Guid ChildProfileId,
    Guid? SchoolId,
    string? SchoolSlug,
    Guid SchoolBranchId,
    Guid EducationalStageId,
    Guid GradeId,
    Guid AcademicYearId,
    string? ParentNotes);

public sealed record UpdateAdmissionApplicationRequest(
    Guid SchoolBranchId,
    Guid EducationalStageId,
    Guid GradeId,
    Guid AcademicYearId,
    string? ParentNotes,
    byte[]? RowVersion,
    bool ConfirmClearQuestionAnswers = false);

public sealed record CancelAdmissionApplicationRequest(string? Reason);

public sealed record AdmissionApplicationListItemDto(
    Guid Id,
    string ApplicationNumber,
    AdmissionApplicationStatus Status,
    Guid ChildProfileId,
    string ChildDisplayName,
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
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc);

public sealed record AdmissionApplicationDetailDto(
    Guid Id,
    string ApplicationNumber,
    AdmissionApplicationStatus Status,
    Guid ChildProfileId,
    string ChildDisplayName,
    string? ChildMaskedIdentity,
    Guid SchoolId,
    string SchoolSlug,
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
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewStartedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RejectedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason,
    string? ParentVisibleRejectionReason,
    IReadOnlyList<AdmissionAttachmentDto> Attachments,
    IReadOnlyList<AdmissionHistoryDto> Timeline,
    IReadOnlyList<AdmissionRequirementChecklistItemDto> Requirements,
    IReadOnlyList<AdmissionQuestionChecklistItemDto> Questions,
    AdmissionMissingItemsRequestDto? ActiveMissingItemsRequest,
    AdmissionAppointmentDto? ActiveInterview,
    AdmissionAppointmentDto? ActiveAssessment,
    AdmissionWaitingListDto? WaitingList,
    DateTimeOffset? RegisteredAtUtc,
    SafeInterviewAssessmentPolicySummaryDto? PolicySummary,
    IReadOnlyList<PublicInterviewFaqItemDto> InterviewFaqs,
    AgeEligibilityResultDto? AgeEligibility,
    AdmissionApplicationCapabilitiesDto Capabilities,
    byte[] RowVersion);

public sealed record AgeEligibilityResultDto(
    ChildAgeEligibilityResultCode ResultCode,
    int? CalculatedAgeCompletedMonths,
    int? MinAgeCompletedMonths,
    int? MaxAgeCompletedMonths,
    DateOnly? ReferenceDate,
    ChildAgeReferenceDateMode? ReferenceDateMode,
    Guid? SourceRuleId,
    int? RuleVersion,
    string? Explanation,
    bool ManualExceptionAllowed,
    bool ManualExceptionIsApproved,
    ChildAgeEligibilityExceptionReasonCode? ManualExceptionReasonCode,
    bool CanContinue,
    bool CanRequestAgeException);

public sealed record AdmissionApplicationCapabilitiesDto(
    bool CanEdit,
    bool CanSubmit,
    bool CanCancel,
    bool CanUploadAttachments,
    bool CanRemoveAttachments,
    bool CanEditRequestedItems,
    bool CanResubmitMissingItems,
    bool CanViewInterview,
    bool CanViewAssessment,
    bool CanRequestAgeException);

public sealed record AdmissionAttachmentDto(
    Guid Id,
    AdmissionAttachmentType AttachmentType,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc,
    Guid? RequirementSnapshotId,
    Guid? QuestionSnapshotId,
    AdmissionRequiredDocumentCode? RequiredDocumentCode);

public sealed record AdmissionHistoryDto(
    Guid Id,
    AdmissionApplicationStatus? FromStatus,
    AdmissionApplicationStatus ToStatus,
    string Action,
    string? ParentVisibleNote,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Carries a private attachment stream to the download endpoint.
/// The stream is owned by the caller (controller) and must be disposed after the response is written.
/// </summary>
public sealed record AdmissionAttachmentDownloadDto(
    Stream Content,
    string ContentType,
    string DownloadFileName,
    long FileSizeBytes);

public sealed record AdmissionApplicationListQuery(
    AdmissionApplicationStatus? Status,
    Guid? ChildProfileId,
    Guid? SchoolId,
    Guid? AcademicYearId,
    string? Search,
    string Sort,
    int PageNumber,
    int PageSize);

public static class AdmissionCapabilityFactory
{
    public static AdmissionApplicationCapabilitiesDto From(
        AdmissionApplicationStatus status,
        DateTimeOffset? reviewStartedAtUtc) =>
        new(
            CanEdit: AdmissionTransitionPolicy.CanParentEdit(status),
            CanSubmit: AdmissionTransitionPolicy.CanParentSubmit(status),
            CanCancel: AdmissionTransitionPolicy.CanParentCancel(status, reviewStartedAtUtc),
            CanUploadAttachments: AdmissionTransitionPolicy.CanParentUploadAttachments(status),
            CanRemoveAttachments: AdmissionTransitionPolicy.CanParentRemoveAttachments(status),
            CanEditRequestedItems: AdmissionTransitionPolicy.CanParentEditRequestedItems(status),
            CanResubmitMissingItems: AdmissionTransitionPolicy.CanParentResubmitMissingItems(status),
            CanViewInterview: status is AdmissionApplicationStatus.InterviewRequired
                or AdmissionApplicationStatus.UnderReview
                or AdmissionApplicationStatus.WaitingList
                or AdmissionApplicationStatus.Accepted
                or AdmissionApplicationStatus.Rejected
                or AdmissionApplicationStatus.Registered
                or AdmissionApplicationStatus.AssessmentRequired,
            CanViewAssessment: status is AdmissionApplicationStatus.AssessmentRequired
                or AdmissionApplicationStatus.UnderReview
                or AdmissionApplicationStatus.WaitingList
                or AdmissionApplicationStatus.Accepted
                or AdmissionApplicationStatus.Rejected
                or AdmissionApplicationStatus.Registered
                or AdmissionApplicationStatus.InterviewRequired,
            CanRequestAgeException: false);
}
