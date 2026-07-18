using Schoolera.Application.Admissions.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Common;

internal static class SchoolAdmissionQuestionMapping
{
    public static SchoolAdmissionQuestionListItemDto ToListItem(SchoolAdmissionQuestion question) =>
        new(
            question.Id,
            question.QuestionCode,
            question.QuestionType,
            question.LabelAr,
            question.LabelEn,
            question.IsRequired,
            question.SortOrder,
            question.PublicationStatus,
            question.IsActive,
            question.SchoolBranchId,
            question.EducationalStageId,
            question.GradeId,
            question.AcademicYearId,
            question.UpdatedAtUtc);

    public static SchoolAdmissionQuestionDetailDto ToDetail(SchoolAdmissionQuestion question) =>
        new(
            question.Id,
            question.QuestionCode,
            question.QuestionType,
            question.LabelAr,
            question.LabelEn,
            question.HelpAr,
            question.HelpEn,
            question.IsRequired,
            question.SortOrder,
            question.PublicationStatus,
            question.IsActive,
            question.SchoolBranchId,
            question.EducationalStageId,
            question.GradeId,
            question.AcademicYearId,
            question.ScopeKey,
            question.SpecificityScore,
            question.MinLength,
            question.MaxLength,
            question.MinSelectedOptions,
            question.MaxSelectedOptions,
            question.MinDate,
            question.MaxDate,
            AdmissionRequirementCatalog.ParseExtensions(question.AllowedFileExtensions),
            question.MaxFileSizeBytes,
            question.AllowChildVaultCopy,
            question.Options
                .OrderBy(option => option.SortOrder)
                .ThenBy(option => option.OptionCode)
                .Select(option => new SchoolAdmissionQuestionOptionDto(
                    option.OptionCode,
                    option.LabelAr,
                    option.LabelEn,
                    option.SortOrder,
                    option.IsActive))
                .ToArray(),
            question.CreatedAtUtc,
            question.UpdatedAtUtc,
            question.PublishedAtUtc);

    public static string? ValidateForPublish(
        SchoolAdmissionQuestion question,
        long platformMaxBytes)
    {
        if (question.SpecificityScore <= 0)
        {
            return SchoolPortalErrorCodes.AdmissionQuestionInvalidScope;
        }

        var errors = AdmissionQuestionCatalog.ValidateForPublish(question);
        if (errors.Count > 0)
        {
            return errors.Contains("invalidFileTypes") || errors.Contains("invalidMaxSize")
                ? SchoolPortalErrorCodes.AdmissionQuestionInvalidOptions
                : SchoolPortalErrorCodes.AdmissionQuestionPublishInvalid;
        }

        if (question.QuestionType == AdmissionQuestionType.File &&
            question.MaxFileSizeBytes is { } max &&
            (max <= 0 || max > platformMaxBytes))
        {
            return SchoolPortalErrorCodes.AdmissionQuestionInvalidOptions;
        }

        return null;
    }

    public static IReadOnlyList<SchoolAdmissionQuestionOption> ToDomainOptions(
        Guid questionId,
        IReadOnlyList<SchoolAdmissionQuestionOptionDto>? options)
    {
        if (options is null || options.Count == 0)
        {
            return [];
        }

        return options
            .Select(option => new SchoolAdmissionQuestionOption(
                questionId,
                option.OptionCode,
                option.LabelAr,
                option.LabelEn,
                option.SortOrder,
                option.IsActive))
            .ToArray();
    }
}
