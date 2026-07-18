using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// School-owned admission requirement definition. Mutable until published snapshots are taken per draft.
/// </summary>
public sealed class SchoolAdmissionRequirement
{
    private SchoolAdmissionRequirement()
    {
    }

    public SchoolAdmissionRequirement(
        Guid schoolId,
        string requirementCode,
        AdmissionRequirementKind kind,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        bool isRequired,
        int sortOrder,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        AdmissionProfileFieldCode? profileFieldCode,
        AdmissionRequiredDocumentCode? documentCode,
        string? allowedFileExtensions,
        long? maxFileSizeBytes,
        bool allowChildVaultCopy,
        Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        RequirementCode = NormalizeCode(requirementCode);
        Kind = kind;
        NameAr = NormalizeRequired(nameAr, FieldLengthLimits.AdmissionRequirementName);
        NameEn = NormalizeRequired(nameEn, FieldLengthLimits.AdmissionRequirementName);
        DescriptionAr = NormalizeOptional(descriptionAr, FieldLengthLimits.AdmissionRequirementDescription);
        DescriptionEn = NormalizeOptional(descriptionEn, FieldLengthLimits.AdmissionRequirementDescription);
        IsRequired = isRequired;
        SortOrder = sortOrder;
        PublicationStatus = AdmissionRequirementPublicationStatus.Draft;
        IsActive = true;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        ProfileFieldCode = profileFieldCode;
        DocumentCode = documentCode;
        AllowedFileExtensions = NormalizeOptional(allowedFileExtensions, FieldLengthLimits.AdmissionRequirementAllowedFiles);
        MaxFileSizeBytes = maxFileSizeBytes;
        AllowChildVaultCopy = allowChildVaultCopy;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    /// <summary>Stable school-scoped code (not localized display text).</summary>
    public string RequirementCode { get; private set; } = string.Empty;

    public AdmissionRequirementKind Kind { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public bool IsRequired { get; private set; }

    public int SortOrder { get; private set; }

    public AdmissionRequirementPublicationStatus PublicationStatus { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? SchoolBranchId { get; private set; }

    public Guid? EducationalStageId { get; private set; }

    public Guid? GradeId { get; private set; }

    public Guid? AcademicYearId { get; private set; }

    /// <summary>Deterministic scope key for uniqueness (empty segments for null scopes).</summary>
    public string ScopeKey { get; private set; } = string.Empty;

    public AdmissionProfileFieldCode? ProfileFieldCode { get; private set; }

    public AdmissionRequiredDocumentCode? DocumentCode { get; private set; }

    /// <summary>Semicolon-separated extensions subset of platform allowlist (e.g. .pdf;.jpg).</summary>
    public string? AllowedFileExtensions { get; private set; }

    public long? MaxFileSizeBytes { get; private set; }

    public bool AllowChildVaultCopy { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public void UpdateDraft(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        bool isRequired,
        int sortOrder,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        AdmissionProfileFieldCode? profileFieldCode,
        AdmissionRequiredDocumentCode? documentCode,
        string? allowedFileExtensions,
        long? maxFileSizeBytes,
        bool allowChildVaultCopy,
        Guid updatedByUserId)
    {
        EnsureDraftEditable();
        NameAr = NormalizeRequired(nameAr, FieldLengthLimits.AdmissionRequirementName);
        NameEn = NormalizeRequired(nameEn, FieldLengthLimits.AdmissionRequirementName);
        DescriptionAr = NormalizeOptional(descriptionAr, FieldLengthLimits.AdmissionRequirementDescription);
        DescriptionEn = NormalizeOptional(descriptionEn, FieldLengthLimits.AdmissionRequirementDescription);
        IsRequired = isRequired;
        SortOrder = sortOrder;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        ProfileFieldCode = profileFieldCode;
        DocumentCode = documentCode;
        AllowedFileExtensions = NormalizeOptional(allowedFileExtensions, FieldLengthLimits.AdmissionRequirementAllowedFiles);
        MaxFileSizeBytes = maxFileSizeBytes;
        AllowChildVaultCopy = allowChildVaultCopy;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
        Touch(updatedByUserId);
    }

    public void SetSortOrder(int sortOrder, Guid updatedByUserId)
    {
        SortOrder = sortOrder;
        Touch(updatedByUserId);
    }

    public void Publish(Guid updatedByUserId)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Inactive requirements cannot be published.");
        }

        PublicationStatus = AdmissionRequirementPublicationStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedByUserId);
    }

    public void Unpublish(Guid updatedByUserId)
    {
        PublicationStatus = AdmissionRequirementPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Deactivate(Guid updatedByUserId)
    {
        IsActive = false;
        PublicationStatus = AdmissionRequirementPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Activate(Guid updatedByUserId)
    {
        IsActive = true;
        Touch(updatedByUserId);
    }

    /// <summary>
    /// Specificity score aligned with documented precedence (higher = more specific).
    /// 7 Branch+Grade+Year, 6 Branch+Stage+Year, 5 Grade+Year, 4 Stage+Year,
    /// 3 Branch+Year, 2 Year only, 1 School default.
    /// </summary>
    public int SpecificityScore =>
        AdmissionScope.ComputeSpecificityScore(SchoolBranchId, EducationalStageId, GradeId, AcademicYearId);

    public static int ComputeSpecificityScore(
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId) =>
        AdmissionScope.ComputeSpecificityScore(branchId, stageId, gradeId, academicYearId);

    public static string BuildScopeKey(
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId) =>
        AdmissionScope.BuildScopeKey(branchId, stageId, gradeId, academicYearId);

    public static string NormalizeCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > FieldLengthLimits.AdmissionRequirementCode)
        {
            throw new ArgumentException("Requirement code is invalid.", nameof(code));
        }

        return normalized;
    }

    private void EnsureDraftEditable()
    {
        if (PublicationStatus == AdmissionRequirementPublicationStatus.Published)
        {
            throw new InvalidOperationException("Published requirements must be unpublished before structural edits.");
        }
    }

    private void Touch(Guid userId)
    {
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Required text cannot be empty.");
        }

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
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
