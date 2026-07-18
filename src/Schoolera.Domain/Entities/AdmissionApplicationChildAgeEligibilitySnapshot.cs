using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// One-per-application snapshot of the resolved child age eligibility evaluation.
/// Stores DateOnly birth snapshot only (no national ID). Age months are derived, not a mutable child field.
/// </summary>
public sealed class AdmissionApplicationChildAgeEligibilitySnapshot
{
    private AdmissionApplicationChildAgeEligibilitySnapshot()
    {
    }

    private AdmissionApplicationChildAgeEligibilitySnapshot(
        Guid admissionApplicationId,
        Guid? sourceRuleId,
        int ruleVersion,
        Guid schoolBranchId,
        Guid educationalStageId,
        Guid gradeId,
        Guid academicYearId,
        int minAgeCompletedMonths,
        int maxAgeCompletedMonths,
        DateOnly referenceDate,
        ChildAgeReferenceDateMode referenceDateMode,
        int? calculatedAgeCompletedMonths,
        ChildAgeEligibilityResultCode resultCode,
        DateOnly? birthDateSnapshot,
        bool manualExceptionAllowedAtEvaluation)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        SourceRuleId = sourceRuleId;
        RuleVersion = ruleVersion;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        MinAgeCompletedMonths = minAgeCompletedMonths;
        MaxAgeCompletedMonths = maxAgeCompletedMonths;
        ReferenceDate = referenceDate;
        ReferenceDateMode = referenceDateMode;
        CalculatedAgeCompletedMonths = calculatedAgeCompletedMonths;
        ResultCode = resultCode;
        BirthDateSnapshot = birthDateSnapshot;
        ManualExceptionAllowedAtEvaluation = manualExceptionAllowedAtEvaluation;
        ManualExceptionIsApproved = false;
        CalculatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static AdmissionApplicationChildAgeEligibilitySnapshot FromRule(
        Guid admissionApplicationId,
        SchoolChildAgeEligibilityRule rule,
        Guid applicationBranchId,
        Guid applicationStageId,
        Guid applicationGradeId,
        Guid applicationAcademicYearId,
        DateOnly referenceDate,
        int? calculatedAgeCompletedMonths,
        ChildAgeEligibilityResultCode resultCode,
        DateOnly? birthDateSnapshot) =>
        new(
            admissionApplicationId,
            rule.Id,
            rule.RuleVersion,
            applicationBranchId,
            applicationStageId,
            applicationGradeId,
            applicationAcademicYearId,
            rule.MinAgeCompletedMonths,
            rule.MaxAgeCompletedMonths,
            referenceDate,
            rule.ReferenceDateMode,
            calculatedAgeCompletedMonths,
            resultCode,
            birthDateSnapshot,
            rule.ManualExceptionAllowed);

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public AdmissionApplication AdmissionApplication { get; private set; } = null!;

    public Guid? SourceRuleId { get; private set; }

    public int RuleVersion { get; private set; }

    public Guid SchoolBranchId { get; private set; }

    public Guid EducationalStageId { get; private set; }

    public Guid GradeId { get; private set; }

    public Guid AcademicYearId { get; private set; }

    public int MinAgeCompletedMonths { get; private set; }

    public int MaxAgeCompletedMonths { get; private set; }

    public DateOnly ReferenceDate { get; private set; }

    public ChildAgeReferenceDateMode ReferenceDateMode { get; private set; }

    public int? CalculatedAgeCompletedMonths { get; private set; }

    public ChildAgeEligibilityResultCode ResultCode { get; private set; }

    public DateTimeOffset CalculatedAtUtc { get; private set; }

    public bool ManualExceptionAllowedAtEvaluation { get; private set; }

    public bool ManualExceptionIsApproved { get; private set; }

    public Guid? ManualExceptionApprovedByUserId { get; private set; }

    public ChildAgeEligibilityExceptionReasonCode? ManualExceptionReasonCode { get; private set; }

    public string? ManualExceptionReasonNote { get; private set; }

    public DateTimeOffset? ManualExceptionApprovedAtUtc { get; private set; }

    /// <summary>DateOnly birth used at evaluation — no national ID.</summary>
    public DateOnly? BirthDateSnapshot { get; private set; }

    public void Recalculate(
        Guid? sourceRuleId,
        int ruleVersion,
        Guid schoolBranchId,
        Guid educationalStageId,
        Guid gradeId,
        Guid academicYearId,
        int minAgeCompletedMonths,
        int maxAgeCompletedMonths,
        DateOnly referenceDate,
        ChildAgeReferenceDateMode referenceDateMode,
        int? calculatedAgeCompletedMonths,
        ChildAgeEligibilityResultCode resultCode,
        DateOnly? birthDateSnapshot,
        bool manualExceptionAllowedAtEvaluation)
    {
        SourceRuleId = sourceRuleId;
        RuleVersion = ruleVersion;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        MinAgeCompletedMonths = minAgeCompletedMonths;
        MaxAgeCompletedMonths = maxAgeCompletedMonths;
        ReferenceDate = referenceDate;
        ReferenceDateMode = referenceDateMode;
        CalculatedAgeCompletedMonths = calculatedAgeCompletedMonths;
        ResultCode = resultCode;
        BirthDateSnapshot = birthDateSnapshot;
        ManualExceptionAllowedAtEvaluation = manualExceptionAllowedAtEvaluation;
        CalculatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ApplyManualException(
        Guid approvedByUserId,
        ChildAgeEligibilityExceptionReasonCode reasonCode,
        string? reasonNote)
    {
        if (!Enum.IsDefined(reasonCode))
        {
            throw new ArgumentOutOfRangeException(nameof(reasonCode));
        }

        ManualExceptionIsApproved = true;
        ManualExceptionApprovedByUserId = approvedByUserId;
        ManualExceptionReasonCode = reasonCode;
        ManualExceptionReasonNote = TruncateOptional(reasonNote, FieldLengthLimits.AgeEligibilityExceptionNote);
        ManualExceptionApprovedAtUtc = DateTimeOffset.UtcNow;
        ResultCode = ChildAgeEligibilityResultCode.ManualExceptionApproved;
    }

    public void ClearManualException()
    {
        ManualExceptionIsApproved = false;
        ManualExceptionApprovedByUserId = null;
        ManualExceptionReasonCode = null;
        ManualExceptionReasonNote = null;
        ManualExceptionApprovedAtUtc = null;
    }

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
