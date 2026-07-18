using Schoolera.Application.Cms.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Dtos;

public sealed record CreateSchoolInterviewFaqRequest(
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    InterviewFaqCategory InterviewCategory,
    Guid? SchoolBranchId = null,
    Guid? EducationalStageId = null,
    Guid? GradeId = null,
    Guid? AcademicYearId = null);

public sealed record UpdateSchoolInterviewFaqRequest(
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    InterviewFaqCategory InterviewCategory,
    Guid? SchoolBranchId = null,
    Guid? EducationalStageId = null,
    Guid? GradeId = null,
    Guid? AcademicYearId = null,
    byte[]? RowVersion = null);

public sealed record ReorderSchoolInterviewFaqsRequest(IReadOnlyList<Guid> OrderedIds);

/// <summary>School-portal admin view of an interview FAQ (reuses CMS admin DTO shape).</summary>
public sealed record SchoolInterviewFaqDetailDto(
    Guid Id,
    Guid FaqCategoryId,
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    int SortOrder,
    bool IsPublished,
    bool IsActive,
    FaqOwnershipScope OwnershipScope,
    Guid SchoolId,
    InterviewFaqCategory InterviewCategory,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion)
{
    public static SchoolInterviewFaqDetailDto FromAdmin(FaqItemAdminDto dto) =>
        new(
            dto.Id,
            dto.FaqCategoryId,
            dto.QuestionAr,
            dto.QuestionEn,
            dto.AnswerAr,
            dto.AnswerEn,
            dto.SortOrder,
            dto.IsPublished,
            dto.IsActive,
            dto.OwnershipScope,
            dto.SchoolId ?? Guid.Empty,
            dto.InterviewCategory ?? InterviewFaqCategory.Interview,
            dto.SchoolBranchId,
            dto.EducationalStageId,
            dto.GradeId,
            dto.AcademicYearId,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            dto.RowVersion);
}
