using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Common;

/// <summary>
/// Resolves whether an interview FAQ applies to a parent/public selection context.
/// </summary>
public static class InterviewFaqApplicability
{
    public static bool MatchesCategoryFilter(
        InterviewFaqCategory itemCategory,
        InterviewFaqCategory? filter)
    {
        if (filter is null)
        {
            return true;
        }

        if (itemCategory == filter)
        {
            return true;
        }

        // Dual-purpose FAQs appear for either Interview or Assessment filters.
        return itemCategory == InterviewFaqCategory.InterviewAndAssessment &&
               filter is InterviewFaqCategory.Interview or InterviewFaqCategory.Assessment;
    }

    /// <summary>
    /// Platform interview FAQs always match when published+active+InterviewCategory set.
    /// School FAQs use <see cref="AdmissionScope.MatchesScope"/> when all context IDs are present;
    /// if a context dimension is missing, the FAQ is included only when that definition dimension is also null.
    /// </summary>
    public static bool Matches(
        FaqItem item,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId)
    {
        if (item.InterviewCategory is null || !item.IsPublished || !item.IsActive)
        {
            return false;
        }

        if (item.OwnershipScope == FaqOwnershipScope.Platform)
        {
            return true;
        }

        if (item.OwnershipScope != FaqOwnershipScope.School || item.SchoolId is null)
        {
            return false;
        }

        if (schoolBranchId is { } branch &&
            educationalStageId is { } stage &&
            gradeId is { } grade &&
            academicYearId is { } year)
        {
            return AdmissionScope.MatchesScope(
                item.SchoolBranchId,
                item.EducationalStageId,
                item.GradeId,
                item.AcademicYearId,
                branch,
                stage,
                grade,
                year);
        }

        return MatchesOptionalDimension(item.SchoolBranchId, schoolBranchId) &&
               MatchesOptionalDimension(item.EducationalStageId, educationalStageId) &&
               MatchesOptionalDimension(item.GradeId, gradeId) &&
               MatchesOptionalDimension(item.AcademicYearId, academicYearId);
    }

    /// <summary>
    /// When context dimension is missing, definition must also be null.
    /// When context is present, definition may be null (wildcard) or equal.
    /// </summary>
    private static bool MatchesOptionalDimension(Guid? definitionId, Guid? contextId)
    {
        if (!contextId.HasValue)
        {
            return !definitionId.HasValue;
        }

        return !definitionId.HasValue || definitionId.Value == contextId.Value;
    }
}
