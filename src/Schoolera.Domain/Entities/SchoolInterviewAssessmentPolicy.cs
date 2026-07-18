using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// School-owned interview/assessment policy. One published active policy applies per application scope.
/// </summary>
public sealed class SchoolInterviewAssessmentPolicy
{
    private SchoolInterviewAssessmentPolicy()
    {
    }

    public SchoolInterviewAssessmentPolicy(
        Guid schoolId,
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
        HybridDeliverySelectionAuthority? hybridSelectionAuthority,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        PublicationStatus = InterviewAssessmentPolicyPublicationStatus.Draft;
        IsActive = true;
        PolicyVersion = 1;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        ApplyModeFields(
            requirementMode,
            deliveryMode,
            requiredParticipants,
            expectedDurationMinutes,
            bookingWindowOpensDaysBefore,
            bookingWindowClosesDaysBefore,
            minimumSchedulingLeadTimeHours,
            parentReschedulingAllowed,
            maxParentRescheduleAttempts,
            parentCancellationAllowed,
            preparationNotesAr,
            preparationNotesEn,
            onSiteInstructionsAr,
            onSiteInstructionsEn,
            onlineInstructionsAr,
            onlineInstructionsEn,
            meetingProviderCode,
            hybridSelectionAuthority);
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public Guid? SchoolBranchId { get; private set; }

    public Guid? EducationalStageId { get; private set; }

    public Guid? GradeId { get; private set; }

    public Guid? AcademicYearId { get; private set; }

    public string ScopeKey { get; private set; } = string.Empty;

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

    public InterviewAssessmentPolicyPublicationStatus PublicationStatus { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Starts at 1; increments on each successful Publish.</summary>
    public int PolicyVersion { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = null!;

    public int SpecificityScore =>
        AdmissionScope.ComputeSpecificityScore(SchoolBranchId, EducationalStageId, GradeId, AcademicYearId);

    public void UpdateDraft(
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
        HybridDeliverySelectionAuthority? hybridSelectionAuthority,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        Guid updatedByUserId)
    {
        EnsureDraftEditable();
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
        ApplyModeFields(
            requirementMode,
            deliveryMode,
            requiredParticipants,
            expectedDurationMinutes,
            bookingWindowOpensDaysBefore,
            bookingWindowClosesDaysBefore,
            minimumSchedulingLeadTimeHours,
            parentReschedulingAllowed,
            maxParentRescheduleAttempts,
            parentCancellationAllowed,
            preparationNotesAr,
            preparationNotesEn,
            onSiteInstructionsAr,
            onSiteInstructionsEn,
            onlineInstructionsAr,
            onlineInstructionsEn,
            meetingProviderCode,
            hybridSelectionAuthority);
        Touch(updatedByUserId);
    }

    public void Publish(Guid updatedByUserId)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Inactive policies cannot be published.");
        }

        // First publish keeps version 1; each later publish cycle increments.
        if (PublishedAtUtc is not null)
        {
            PolicyVersion += 1;
        }

        PublicationStatus = InterviewAssessmentPolicyPublicationStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedByUserId);
    }

    public void Unpublish(Guid updatedByUserId)
    {
        PublicationStatus = InterviewAssessmentPolicyPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Deactivate(Guid updatedByUserId)
    {
        IsActive = false;
        PublicationStatus = InterviewAssessmentPolicyPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Activate(Guid updatedByUserId)
    {
        IsActive = true;
        Touch(updatedByUserId);
    }

    /// <summary>Creates a new Draft copy with PolicyVersion reset to 1.</summary>
    public SchoolInterviewAssessmentPolicy CloneAsDraft(Guid newCreatorUserId) =>
        new(
            SchoolId,
            RequirementMode,
            DeliveryMode,
            RequiredParticipants,
            ExpectedDurationMinutes,
            BookingWindowOpensDaysBefore,
            BookingWindowClosesDaysBefore,
            MinimumSchedulingLeadTimeHours,
            ParentReschedulingAllowed,
            MaxParentRescheduleAttempts,
            ParentCancellationAllowed,
            PreparationNotesAr,
            PreparationNotesEn,
            OnSiteInstructionsAr,
            OnSiteInstructionsEn,
            OnlineInstructionsAr,
            OnlineInstructionsEn,
            MeetingProviderCode,
            HybridSelectionAuthority,
            SchoolBranchId,
            EducationalStageId,
            GradeId,
            AcademicYearId,
            newCreatorUserId);

    private void ApplyModeFields(
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
        if (!Enum.IsDefined(requirementMode))
        {
            throw new ArgumentOutOfRangeException(nameof(requirementMode));
        }

        RequirementMode = requirementMode;
        PreparationNotesAr = NormalizeOptional(preparationNotesAr, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        PreparationNotesEn = NormalizeOptional(preparationNotesEn, FieldLengthLimits.InterviewAssessmentPolicyNotes);

        if (requirementMode == InterviewAssessmentRequirementMode.NotRequired)
        {
            DeliveryMode = null;
            RequiredParticipants = null;
            ExpectedDurationMinutes = null;
            BookingWindowOpensDaysBefore = null;
            BookingWindowClosesDaysBefore = null;
            MinimumSchedulingLeadTimeHours = null;
            ParentReschedulingAllowed = false;
            MaxParentRescheduleAttempts = 0;
            ParentCancellationAllowed = false;
            MeetingProviderCode = null;
            HybridSelectionAuthority = null;
            OnSiteInstructionsAr = null;
            OnSiteInstructionsEn = null;
            OnlineInstructionsAr = null;
            OnlineInstructionsEn = null;
            return;
        }

        DeliveryMode = deliveryMode;
        RequiredParticipants = requiredParticipants;
        ExpectedDurationMinutes = expectedDurationMinutes;
        BookingWindowOpensDaysBefore = bookingWindowOpensDaysBefore;
        BookingWindowClosesDaysBefore = bookingWindowClosesDaysBefore;
        MinimumSchedulingLeadTimeHours = minimumSchedulingLeadTimeHours;
        ParentReschedulingAllowed = parentReschedulingAllowed;
        MaxParentRescheduleAttempts = maxParentRescheduleAttempts < 0 ? 0 : maxParentRescheduleAttempts;
        ParentCancellationAllowed = parentCancellationAllowed;
        MeetingProviderCode = NormalizeOptional(meetingProviderCode, FieldLengthLimits.MeetingProviderCode);
        HybridSelectionAuthority = hybridSelectionAuthority;
        OnSiteInstructionsAr = NormalizeOptional(onSiteInstructionsAr, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        OnSiteInstructionsEn = NormalizeOptional(onSiteInstructionsEn, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        OnlineInstructionsAr = NormalizeOptional(onlineInstructionsAr, FieldLengthLimits.InterviewAssessmentPolicyNotes);
        OnlineInstructionsEn = NormalizeOptional(onlineInstructionsEn, FieldLengthLimits.InterviewAssessmentPolicyNotes);
    }

    private void EnsureDraftEditable()
    {
        if (PublicationStatus == InterviewAssessmentPolicyPublicationStatus.Published)
        {
            throw new InvalidOperationException("Published policies must be unpublished before structural edits.");
        }
    }

    private void Touch(Guid userId)
    {
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
