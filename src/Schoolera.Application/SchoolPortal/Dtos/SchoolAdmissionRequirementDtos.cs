using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Dtos;

public sealed record SchoolAdmissionRequirementListItemDto(
    Guid Id,
    string RequirementCode,
    AdmissionRequirementKind Kind,
    string NameAr,
    string NameEn,
    bool IsRequired,
    int SortOrder,
    AdmissionRequirementPublicationStatus PublicationStatus,
    bool IsActive,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    DateTimeOffset UpdatedAtUtc);

public sealed record SchoolAdmissionRequirementDetailDto(
    Guid Id,
    string RequirementCode,
    AdmissionRequirementKind Kind,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsRequired,
    int SortOrder,
    AdmissionRequirementPublicationStatus PublicationStatus,
    bool IsActive,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    string ScopeKey,
    int SpecificityScore,
    AdmissionProfileFieldCode? ProfileFieldCode,
    AdmissionRequiredDocumentCode? DocumentCode,
    IReadOnlyList<string> AllowedFileExtensions,
    long? MaxFileSizeBytes,
    bool AllowChildVaultCopy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);

public sealed record CreateSchoolAdmissionRequirementRequest(
    string RequirementCode,
    AdmissionRequirementKind Kind,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsRequired,
    int SortOrder,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    AdmissionProfileFieldCode? ProfileFieldCode,
    AdmissionRequiredDocumentCode? DocumentCode,
    IReadOnlyList<string>? AllowedFileExtensions,
    long? MaxFileSizeBytes,
    bool AllowChildVaultCopy);

public sealed record UpdateSchoolAdmissionRequirementRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsRequired,
    int SortOrder,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    AdmissionProfileFieldCode? ProfileFieldCode,
    AdmissionRequiredDocumentCode? DocumentCode,
    IReadOnlyList<string>? AllowedFileExtensions,
    long? MaxFileSizeBytes,
    bool AllowChildVaultCopy);

public sealed record ReorderSchoolAdmissionRequirementsRequest(IReadOnlyList<Guid> OrderedRequirementIds);
