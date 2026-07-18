using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Resolves the single applicable published child age eligibility rule for a selection.</summary>
public static class ChildAgeEligibilityCatalog
{
    public const int MinCompletedMonths = 0;
    public const int MaxCompletedMonths = SchoolChildAgeEligibilityRule.MaxCompletedMonths;

    public static bool MatchesScope(
        SchoolChildAgeEligibilityRule rule,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId) =>
        AdmissionScope.MatchesScope(
            rule.SchoolBranchId,
            rule.EducationalStageId,
            rule.GradeId,
            rule.AcademicYearId,
            branchId,
            stageId,
            gradeId,
            academicYearId);

    /// <summary>
    /// Exactly one rule applies: most specific published active match, or null.
    /// </summary>
    public static SchoolChildAgeEligibilityRule? ResolveApplicable(
        IEnumerable<SchoolChildAgeEligibilityRule> candidates,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId) =>
        candidates
            .Where(rule =>
                rule.IsActive &&
                rule.PublicationStatus == ChildAgeEligibilityPublicationStatus.Published &&
                MatchesScope(rule, branchId, stageId, gradeId, academicYearId))
            .OrderByDescending(rule => rule.SpecificityScore)
            .ThenBy(rule => rule.Id)
            .FirstOrDefault();

    public static bool IsAgeRangeValid(int minMonths, int maxMonths) =>
        minMonths >= MinCompletedMonths &&
        minMonths <= MaxCompletedMonths &&
        maxMonths >= minMonths &&
        maxMonths <= MaxCompletedMonths;

    /// <summary>
    /// Stage + Academic Year are always required; Branch/Grade optional. Score must be &gt; 0.
    /// </summary>
    public static bool IsValidDefinitionScope(
        Guid? branchId,
        Guid stageId,
        Guid? gradeId,
        Guid academicYearId) =>
        stageId != Guid.Empty &&
        academicYearId != Guid.Empty &&
        AdmissionScope.ComputeSpecificityScore(branchId, stageId, gradeId, academicYearId) > 0;
}
