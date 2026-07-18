using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Immutable per-application copy of a school requirement resolved at draft creation (or one-time backfill).
/// </summary>
public sealed class AdmissionApplicationRequirementSnapshot
{
    private AdmissionApplicationRequirementSnapshot()
    {
    }

    public AdmissionApplicationRequirementSnapshot(
        Guid admissionApplicationId,
        Guid? sourceRequirementId,
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
        bool allowChildVaultCopy)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        SourceRequirementId = sourceRequirementId;
        RequirementCode = requirementCode.Trim().ToLowerInvariant();
        Kind = kind;
        NameAr = Truncate(nameAr, FieldLengthLimits.AdmissionRequirementName);
        NameEn = Truncate(nameEn, FieldLengthLimits.AdmissionRequirementName);
        DescriptionAr = TruncateOptional(descriptionAr, FieldLengthLimits.AdmissionRequirementDescription);
        DescriptionEn = TruncateOptional(descriptionEn, FieldLengthLimits.AdmissionRequirementDescription);
        IsRequired = isRequired;
        SortOrder = sortOrder;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        ProfileFieldCode = profileFieldCode;
        DocumentCode = documentCode;
        AllowedFileExtensions = TruncateOptional(allowedFileExtensions, FieldLengthLimits.AdmissionRequirementAllowedFiles);
        MaxFileSizeBytes = maxFileSizeBytes;
        AllowChildVaultCopy = allowChildVaultCopy;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static AdmissionApplicationRequirementSnapshot FromDefinition(
        Guid admissionApplicationId,
        SchoolAdmissionRequirement requirement) =>
        new(
            admissionApplicationId,
            requirement.Id,
            requirement.RequirementCode,
            requirement.Kind,
            requirement.NameAr,
            requirement.NameEn,
            requirement.DescriptionAr,
            requirement.DescriptionEn,
            requirement.IsRequired,
            requirement.SortOrder,
            requirement.SchoolBranchId,
            requirement.EducationalStageId,
            requirement.GradeId,
            requirement.AcademicYearId,
            requirement.ProfileFieldCode,
            requirement.DocumentCode,
            requirement.AllowedFileExtensions,
            requirement.MaxFileSizeBytes,
            requirement.AllowChildVaultCopy);

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public AdmissionApplication AdmissionApplication { get; private set; } = null!;

    public Guid? SourceRequirementId { get; private set; }

    public string RequirementCode { get; private set; } = string.Empty;

    public AdmissionRequirementKind Kind { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public bool IsRequired { get; private set; }

    public int SortOrder { get; private set; }

    public Guid? SchoolBranchId { get; private set; }

    public Guid? EducationalStageId { get; private set; }

    public Guid? GradeId { get; private set; }

    public Guid? AcademicYearId { get; private set; }

    public AdmissionProfileFieldCode? ProfileFieldCode { get; private set; }

    public AdmissionRequiredDocumentCode? DocumentCode { get; private set; }

    public string? AllowedFileExtensions { get; private set; }

    public long? MaxFileSizeBytes { get; private set; }

    public bool AllowChildVaultCopy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<AdmissionApplicationAttachment> Attachments { get; private set; } = [];

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? TruncateOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Truncate(value, maxLength);
    }
}
