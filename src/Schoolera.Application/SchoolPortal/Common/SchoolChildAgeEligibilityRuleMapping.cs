using Schoolera.Application.Admissions.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Common;

internal static class SchoolChildAgeEligibilityRuleMapping
{
    public static SchoolChildAgeEligibilityRuleListItemDto ToListItem(SchoolChildAgeEligibilityRule rule) =>
        new(
            rule.Id,
            rule.MinAgeCompletedMonths,
            rule.MaxAgeCompletedMonths,
            rule.ReferenceDateMode,
            rule.PublicationStatus,
            rule.IsActive,
            rule.ManualExceptionAllowed,
            rule.RuleVersion,
            rule.SchoolBranchId,
            rule.EducationalStageId,
            rule.GradeId,
            rule.AcademicYearId,
            rule.SpecificityScore,
            rule.UpdatedAtUtc);

    public static SchoolChildAgeEligibilityRuleDetailDto ToDetail(SchoolChildAgeEligibilityRule rule) =>
        new(
            rule.Id,
            rule.MinAgeCompletedMonths,
            rule.MaxAgeCompletedMonths,
            rule.ReferenceDateMode,
            rule.ExplanationAr,
            rule.ExplanationEn,
            rule.ManualExceptionAllowed,
            rule.PublicationStatus,
            rule.IsActive,
            rule.RuleVersion,
            rule.SchoolBranchId,
            rule.EducationalStageId,
            rule.GradeId,
            rule.AcademicYearId,
            rule.ScopeKey,
            rule.SpecificityScore,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc,
            rule.PublishedAtUtc,
            rule.RowVersion ?? Array.Empty<byte>());

    public static string? ValidateForPublish(SchoolChildAgeEligibilityRule rule)
    {
        if (!ChildAgeEligibilityCatalog.IsValidDefinitionScope(
                rule.SchoolBranchId,
                rule.EducationalStageId,
                rule.GradeId,
                rule.AcademicYearId))
        {
            return SchoolPortalErrorCodes.AgeEligibilityRuleInvalidScope;
        }

        if (!ChildAgeEligibilityCatalog.IsAgeRangeValid(
                rule.MinAgeCompletedMonths,
                rule.MaxAgeCompletedMonths))
        {
            return SchoolPortalErrorCodes.AgeEligibilityRulePublishInvalid;
        }

        if (rule.ReferenceDateMode != ChildAgeReferenceDateMode.AcademicYearStart ||
            !Enum.IsDefined(rule.ReferenceDateMode))
        {
            return SchoolPortalErrorCodes.AgeEligibilityRulePublishInvalid;
        }

        return null;
    }
}
