using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Dtos;

public sealed record SchoolChildAgeEligibilityRuleListItemDto(
    Guid Id,
    int MinAgeCompletedMonths,
    int MaxAgeCompletedMonths,
    ChildAgeReferenceDateMode ReferenceDateMode,
    ChildAgeEligibilityPublicationStatus PublicationStatus,
    bool IsActive,
    bool ManualExceptionAllowed,
    int RuleVersion,
    Guid? SchoolBranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    int SpecificityScore,
    DateTimeOffset UpdatedAtUtc);

public sealed record SchoolChildAgeEligibilityRuleDetailDto(
    Guid Id,
    int MinAgeCompletedMonths,
    int MaxAgeCompletedMonths,
    ChildAgeReferenceDateMode ReferenceDateMode,
    string? ExplanationAr,
    string? ExplanationEn,
    bool ManualExceptionAllowed,
    ChildAgeEligibilityPublicationStatus PublicationStatus,
    bool IsActive,
    int RuleVersion,
    Guid? SchoolBranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    string ScopeKey,
    int SpecificityScore,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    byte[] RowVersion);

public sealed record CreateSchoolChildAgeEligibilityRuleRequest(
    int MinAgeCompletedMonths,
    int MaxAgeCompletedMonths,
    ChildAgeReferenceDateMode ReferenceDateMode,
    string? ExplanationAr,
    string? ExplanationEn,
    bool ManualExceptionAllowed,
    Guid? SchoolBranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId);

public sealed record UpdateSchoolChildAgeEligibilityRuleRequest(
    int MinAgeCompletedMonths,
    int MaxAgeCompletedMonths,
    ChildAgeReferenceDateMode ReferenceDateMode,
    string? ExplanationAr,
    string? ExplanationEn,
    bool ManualExceptionAllowed,
    Guid? SchoolBranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    byte[]? RowVersion);

public sealed record CloneSchoolChildAgeEligibilityRuleRequest();

public sealed record PreviewSchoolChildAgeEligibilityRequest(
    Guid? SchoolBranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    DateOnly? BirthDate);

public sealed record PreviewSchoolChildAgeEligibilityDto(
    ChildAgeEligibilityResultCode ResultCode,
    int? CalculatedAgeCompletedMonths,
    int? MinAgeCompletedMonths,
    int? MaxAgeCompletedMonths,
    DateOnly? ReferenceDate,
    Guid? MatchedRuleId,
    int? RuleVersion,
    bool ManualExceptionAllowed,
    bool CanContinue,
    SchoolChildAgeEligibilityRuleDetailDto? MatchedRule);
