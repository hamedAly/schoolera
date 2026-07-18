using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Dtos;

public sealed record SchoolInterviewAssessmentPolicyListItemDto(
    Guid Id,
    InterviewAssessmentRequirementMode RequirementMode,
    InterviewAssessmentDeliveryMode? DeliveryMode,
    InterviewAssessmentPolicyPublicationStatus PublicationStatus,
    bool IsActive,
    int PolicyVersion,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    int SpecificityScore,
    DateTimeOffset UpdatedAtUtc);

public sealed record SchoolInterviewAssessmentPolicyDetailDto(
    Guid Id,
    InterviewAssessmentRequirementMode RequirementMode,
    InterviewAssessmentDeliveryMode? DeliveryMode,
    InterviewAssessmentRequiredParticipants? RequiredParticipants,
    int? ExpectedDurationMinutes,
    int? BookingWindowOpensDaysBefore,
    int? BookingWindowClosesDaysBefore,
    int? MinimumSchedulingLeadTimeHours,
    bool ParentReschedulingAllowed,
    int MaxParentRescheduleAttempts,
    bool ParentCancellationAllowed,
    string? PreparationNotesAr,
    string? PreparationNotesEn,
    string? OnSiteInstructionsAr,
    string? OnSiteInstructionsEn,
    string? OnlineInstructionsAr,
    string? OnlineInstructionsEn,
    string? MeetingProviderCode,
    HybridDeliverySelectionAuthority? HybridSelectionAuthority,
    InterviewAssessmentPolicyPublicationStatus PublicationStatus,
    bool IsActive,
    int PolicyVersion,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    string ScopeKey,
    int SpecificityScore,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    byte[] RowVersion);

public sealed record CreateSchoolInterviewAssessmentPolicyRequest(
    InterviewAssessmentRequirementMode RequirementMode,
    InterviewAssessmentDeliveryMode? DeliveryMode,
    InterviewAssessmentRequiredParticipants? RequiredParticipants,
    int? ExpectedDurationMinutes,
    int? BookingWindowOpensDaysBefore,
    int? BookingWindowClosesDaysBefore,
    int? MinimumSchedulingLeadTimeHours,
    bool ParentReschedulingAllowed,
    int MaxParentRescheduleAttempts,
    bool ParentCancellationAllowed,
    string? PreparationNotesAr,
    string? PreparationNotesEn,
    string? OnSiteInstructionsAr,
    string? OnSiteInstructionsEn,
    string? OnlineInstructionsAr,
    string? OnlineInstructionsEn,
    string? MeetingProviderCode,
    HybridDeliverySelectionAuthority? HybridSelectionAuthority,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId);

public sealed record UpdateSchoolInterviewAssessmentPolicyRequest(
    InterviewAssessmentRequirementMode RequirementMode,
    InterviewAssessmentDeliveryMode? DeliveryMode,
    InterviewAssessmentRequiredParticipants? RequiredParticipants,
    int? ExpectedDurationMinutes,
    int? BookingWindowOpensDaysBefore,
    int? BookingWindowClosesDaysBefore,
    int? MinimumSchedulingLeadTimeHours,
    bool ParentReschedulingAllowed,
    int MaxParentRescheduleAttempts,
    bool ParentCancellationAllowed,
    string? PreparationNotesAr,
    string? PreparationNotesEn,
    string? OnSiteInstructionsAr,
    string? OnSiteInstructionsEn,
    string? OnlineInstructionsAr,
    string? OnlineInstructionsEn,
    string? MeetingProviderCode,
    HybridDeliverySelectionAuthority? HybridSelectionAuthority,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    byte[]? RowVersion);

public sealed record CloneSchoolInterviewAssessmentPolicyRequest();

public sealed record PreviewSchoolInterviewAssessmentPolicyApplicabilityRequest(
    Guid SchoolBranchId,
    Guid EducationalStageId,
    Guid GradeId,
    Guid AcademicYearId);

public sealed record PreviewSchoolInterviewAssessmentPolicyApplicabilityDto(
    SchoolInterviewAssessmentPolicyDetailDto? MatchedPolicy,
    int? SpecificityScore);

public sealed record SafeMeetingProviderOptionDto(
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn);

/// <summary>Parent/public-safe policy summary. No credentials or meeting URLs.</summary>
public sealed record SafeInterviewAssessmentPolicySummaryDto(
    InterviewAssessmentRequirementMode RequirementMode,
    InterviewAssessmentDeliveryMode? DeliveryMode,
    InterviewAssessmentRequiredParticipants? RequiredParticipants,
    int? ExpectedDurationMinutes,
    int? BookingWindowOpensDaysBefore,
    int? BookingWindowClosesDaysBefore,
    int? MinimumSchedulingLeadTimeHours,
    bool ParentReschedulingAllowed,
    int MaxParentRescheduleAttempts,
    bool ParentCancellationAllowed,
    string? PreparationNotes,
    string? OnSiteInstructions,
    string? OnlineInstructions,
    string? MeetingProviderCode,
    HybridDeliverySelectionAuthority? HybridSelectionAuthority,
    int? PolicyVersion,
    bool IsOnlineCapabilityAvailable);
