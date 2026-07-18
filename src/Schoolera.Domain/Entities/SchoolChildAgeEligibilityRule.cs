using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// School-owned child age eligibility rule. One published active rule applies per application scope.
/// Stage and Academic Year are always required; Branch and Grade are optional.
/// </summary>
public sealed class SchoolChildAgeEligibilityRule
{
    public const int MaxCompletedMonths = 300; // 25 years * 12

    private SchoolChildAgeEligibilityRule()
    {
    }

    public SchoolChildAgeEligibilityRule(
        Guid schoolId,
        Guid educationalStageId,
        Guid academicYearId,
        int minAgeCompletedMonths,
        int maxAgeCompletedMonths,
        ChildAgeReferenceDateMode referenceDateMode,
        string? explanationAr,
        string? explanationEn,
        bool manualExceptionAllowed,
        Guid? schoolBranchId,
        Guid? gradeId,
        Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        EducationalStageId = educationalStageId;
        AcademicYearId = academicYearId;
        SchoolBranchId = schoolBranchId;
        GradeId = gradeId;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
        PublicationStatus = ChildAgeEligibilityPublicationStatus.Draft;
        IsActive = true;
        RuleVersion = 1;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        ApplyAgeFields(
            minAgeCompletedMonths,
            maxAgeCompletedMonths,
            referenceDateMode,
            explanationAr,
            explanationEn,
            manualExceptionAllowed);
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public Guid? SchoolBranchId { get; private set; }

    public Guid EducationalStageId { get; private set; }

    public Guid? GradeId { get; private set; }

    public Guid AcademicYearId { get; private set; }

    public string ScopeKey { get; private set; } = string.Empty;

    public int MinAgeCompletedMonths { get; private set; }

    public int MaxAgeCompletedMonths { get; private set; }

    public ChildAgeReferenceDateMode ReferenceDateMode { get; private set; }

    public string? ExplanationAr { get; private set; }

    public string? ExplanationEn { get; private set; }

    public bool ManualExceptionAllowed { get; private set; }

    public ChildAgeEligibilityPublicationStatus PublicationStatus { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Starts at 1; first publish keeps 1; later publish cycles increment.</summary>
    public int RuleVersion { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = null!;

    public int SpecificityScore =>
        AdmissionScope.ComputeSpecificityScore(SchoolBranchId, EducationalStageId, GradeId, AcademicYearId);

    public void UpdateDraft(
        Guid educationalStageId,
        Guid academicYearId,
        int minAgeCompletedMonths,
        int maxAgeCompletedMonths,
        ChildAgeReferenceDateMode referenceDateMode,
        string? explanationAr,
        string? explanationEn,
        bool manualExceptionAllowed,
        Guid? schoolBranchId,
        Guid? gradeId,
        Guid updatedByUserId)
    {
        EnsureDraftEditable();
        EducationalStageId = educationalStageId;
        AcademicYearId = academicYearId;
        SchoolBranchId = schoolBranchId;
        GradeId = gradeId;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
        ApplyAgeFields(
            minAgeCompletedMonths,
            maxAgeCompletedMonths,
            referenceDateMode,
            explanationAr,
            explanationEn,
            manualExceptionAllowed);
        Touch(updatedByUserId);
    }

    public void Publish(Guid updatedByUserId)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Inactive rules cannot be published.");
        }

        if (EducationalStageId == Guid.Empty || AcademicYearId == Guid.Empty)
        {
            throw new InvalidOperationException("Stage and academic year are required to publish.");
        }

        if (SpecificityScore <= 0)
        {
            throw new InvalidOperationException("Rule scope is invalid for publish.");
        }

        if (PublishedAtUtc is not null)
        {
            RuleVersion += 1;
        }

        PublicationStatus = ChildAgeEligibilityPublicationStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedByUserId);
    }

    public void Unpublish(Guid updatedByUserId)
    {
        PublicationStatus = ChildAgeEligibilityPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Deactivate(Guid updatedByUserId)
    {
        IsActive = false;
        PublicationStatus = ChildAgeEligibilityPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Activate(Guid updatedByUserId)
    {
        IsActive = true;
        Touch(updatedByUserId);
    }

    public SchoolChildAgeEligibilityRule CloneAsDraft(Guid newCreatorUserId) =>
        new(
            SchoolId,
            EducationalStageId,
            AcademicYearId,
            MinAgeCompletedMonths,
            MaxAgeCompletedMonths,
            ReferenceDateMode,
            ExplanationAr,
            ExplanationEn,
            ManualExceptionAllowed,
            SchoolBranchId,
            GradeId,
            newCreatorUserId);

    private void ApplyAgeFields(
        int minAgeCompletedMonths,
        int maxAgeCompletedMonths,
        ChildAgeReferenceDateMode referenceDateMode,
        string? explanationAr,
        string? explanationEn,
        bool manualExceptionAllowed)
    {
        if (!Enum.IsDefined(referenceDateMode))
        {
            throw new ArgumentOutOfRangeException(nameof(referenceDateMode));
        }

        if (minAgeCompletedMonths < 0 || minAgeCompletedMonths > MaxCompletedMonths)
        {
            throw new ArgumentOutOfRangeException(nameof(minAgeCompletedMonths));
        }

        if (maxAgeCompletedMonths < minAgeCompletedMonths || maxAgeCompletedMonths > MaxCompletedMonths)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAgeCompletedMonths));
        }

        MinAgeCompletedMonths = minAgeCompletedMonths;
        MaxAgeCompletedMonths = maxAgeCompletedMonths;
        ReferenceDateMode = referenceDateMode;
        ExplanationAr = NormalizeOptional(explanationAr, FieldLengthLimits.AgeEligibilityExplanation);
        ExplanationEn = NormalizeOptional(explanationEn, FieldLengthLimits.AgeEligibilityExplanation);
        ManualExceptionAllowed = manualExceptionAllowed;
    }

    private void EnsureDraftEditable()
    {
        if (PublicationStatus == ChildAgeEligibilityPublicationStatus.Published)
        {
            throw new InvalidOperationException("Published rules must be unpublished before structural edits.");
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
