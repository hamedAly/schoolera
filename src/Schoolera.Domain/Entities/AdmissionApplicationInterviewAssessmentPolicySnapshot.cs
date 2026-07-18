using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Immutable one-per-application copy of the resolved interview/assessment policy at draft creation (or replace).
/// Never stores credentials, SettingsJson, or live meeting URLs.
/// </summary>
public sealed class AdmissionApplicationInterviewAssessmentPolicySnapshot
{
    private AdmissionApplicationInterviewAssessmentPolicySnapshot()
    {
    }

    private AdmissionApplicationInterviewAssessmentPolicySnapshot(
        Guid admissionApplicationId,
        Guid sourcePolicyId,
        int policyVersion,
        Guid schoolBranchId,
        Guid educationalStageId,
        Guid gradeId,
        Guid academicYearId,
        InterviewAssessmentRequirementMode requirementMode,
        InterviewAssessmentDeliveryMode? deliveryMode,
        InterviewAssessmentRequiredParticipants? requiredParticipants,
        int? expectedDurationMinutes,
        int? bookingWindowOpensDaysBefore,
        int? bookingWindowClosesDaysBefore,
        int? minimumSchedulingLeadTimeHours,
        bool parentReschedulingAllowed,
        int maxParentRescheduleAttempts,
        bool parentCancellationAllowed,
        string? preparationNotesAr,
        string? preparationNotesEn,
        string? onSiteInstructionsAr,
        string? onSiteInstructionsEn,
        string? onlineInstructionsAr,
        string? onlineInstructionsEn,
        string? meetingProviderCode,
        HybridDeliverySelectionAuthority? hybridSelectionAuthority)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        SourcePolicyId = sourcePolicyId;
        PolicyVersion = policyVersion;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        RequirementMode = requirementMode;
        DeliveryMode = deliveryMode;
        RequiredParticipants = requiredParticipants;
        ExpectedDurationMinutes = expectedDurationMinutes;
        BookingWindowOpensDaysBefore = bookingWindowOpensDaysBefore;
        BookingWindowClosesDaysBefore = bookingWindowClosesDaysBefore;
        MinimumSchedulingLeadTimeHours = minimumSchedulingLeadTimeHours;
        ParentReschedulingAllowed = parentReschedulingAllowed;
        MaxParentRescheduleAttempts = maxParentRescheduleAttempts;
        ParentCancellationAllowed = parentCancellationAllowed;
        PreparationNotesAr = TruncateOptional(preparationNotesAr, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        PreparationNotesEn = TruncateOptional(preparationNotesEn, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        OnSiteInstructionsAr = TruncateOptional(onSiteInstructionsAr, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        OnSiteInstructionsEn = TruncateOptional(onSiteInstructionsEn, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        OnlineInstructionsAr = TruncateOptional(onlineInstructionsAr, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        OnlineInstructionsEn = TruncateOptional(onlineInstructionsEn, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        MeetingProviderCode = TruncateOptional(meetingProviderCode, FieldLengthLimits.MeetingProviderCode);
        HybridSelectionAuthority = hybridSelectionAuthority;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Snapshots policy fields plus the application's resolved scope IDs at snapshot time
    /// (not the policy definition's optional scope segments).
    /// </summary>
    public static AdmissionApplicationInterviewAssessmentPolicySnapshot FromPolicy(
        Guid admissionApplicationId,
        SchoolInterviewAssessmentPolicy policy,
        Guid applicationBranchId,
        Guid applicationStageId,
        Guid applicationGradeId,
        Guid applicationAcademicYearId) =>
        new(
            admissionApplicationId,
            policy.Id,
            policy.PolicyVersion,
            applicationBranchId,
            applicationStageId,
            applicationGradeId,
            applicationAcademicYearId,
            policy.RequirementMode,
            policy.DeliveryMode,
            policy.RequiredParticipants,
            policy.ExpectedDurationMinutes,
            policy.BookingWindowOpensDaysBefore,
            policy.BookingWindowClosesDaysBefore,
            policy.MinimumSchedulingLeadTimeHours,
            policy.ParentReschedulingAllowed,
            policy.MaxParentRescheduleAttempts,
            policy.ParentCancellationAllowed,
            policy.PreparationNotesAr,
            policy.PreparationNotesEn,
            policy.OnSiteInstructionsAr,
            policy.OnSiteInstructionsEn,
            policy.OnlineInstructionsAr,
            policy.OnlineInstructionsEn,
            policy.MeetingProviderCode,
            policy.HybridSelectionAuthority);

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public AdmissionApplication AdmissionApplication { get; private set; } = null!;

    public Guid SourcePolicyId { get; private set; }

    public int PolicyVersion { get; private set; }

    public Guid SchoolBranchId { get; private set; }

    public Guid EducationalStageId { get; private set; }

    public Guid GradeId { get; private set; }

    public Guid AcademicYearId { get; private set; }

    public InterviewAssessmentRequirementMode RequirementMode { get; private set; }

    public InterviewAssessmentDeliveryMode? DeliveryMode { get; private set; }

    public InterviewAssessmentRequiredParticipants? RequiredParticipants { get; private set; }

    public int? ExpectedDurationMinutes { get; private set; }

    public int? BookingWindowOpensDaysBefore { get; private set; }

    public int? BookingWindowClosesDaysBefore { get; private set; }

    public int? MinimumSchedulingLeadTimeHours { get; private set; }

    public bool ParentReschedulingAllowed { get; private set; }

    public int MaxParentRescheduleAttempts { get; private set; }

    public bool ParentCancellationAllowed { get; private set; }

    public string? PreparationNotesAr { get; private set; }

    public string? PreparationNotesEn { get; private set; }

    public string? OnSiteInstructionsAr { get; private set; }

    public string? OnSiteInstructionsEn { get; private set; }

    public string? OnlineInstructionsAr { get; private set; }

    public string? OnlineInstructionsEn { get; private set; }

    public string? MeetingProviderCode { get; private set; }

    public HybridDeliverySelectionAuthority? HybridSelectionAuthority { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private static string? TruncateOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
