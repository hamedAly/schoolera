using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public interface IChildAgeEligibilityEvaluator
{
    /// <summary>
    /// Evaluates age eligibility for Admission (and reusable by Transfer when that module exists).
    /// Age is always derived from BirthDate + ReferenceDate — never a persisted mutable Age field.
    /// When no published rule matches: <see cref="ChildAgeEligibilityResultCode.RuleNotConfigured"/> with CanContinue=true.
    /// </summary>
    Task<ChildAgeEligibilityEvaluation> EvaluateAsync(
        Guid schoolId,
        Guid? branchId,
        Guid stageId,
        Guid? gradeId,
        Guid academicYearId,
        DateOnly? birthDate,
        CancellationToken cancellationToken = default);
}

public sealed class ChildAgeEligibilityEvaluator(
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    ITaxonomyRepository taxonomyRepository)
    : IChildAgeEligibilityEvaluator
{
    public async Task<ChildAgeEligibilityEvaluation> EvaluateAsync(
        Guid schoolId,
        Guid? branchId,
        Guid stageId,
        Guid? gradeId,
        Guid academicYearId,
        DateOnly? birthDate,
        CancellationToken cancellationToken = default)
    {
        var published = await ruleRepository.ListPublishedActiveAsync(schoolId, cancellationToken);
        var applicationBranchId = branchId ?? Guid.Empty;
        var applicationGradeId = gradeId ?? Guid.Empty;

        // When branch/grade are unknown (preview), only school-wide Stage+Year rules can match via empty Guid.
        // Callers with full application scope always pass concrete IDs.
        var applicable = ChildAgeEligibilityCatalog.ResolveApplicable(
            published,
            applicationBranchId,
            stageId,
            applicationGradeId,
            academicYearId);

        if (applicable is null)
        {
            return ChildAgeEligibilityEvaluation.RuleNotConfigured();
        }

        var academicYear = await taxonomyRepository.GetAcademicYearByIdAsync(
            academicYearId,
            cancellationToken);
        if (academicYear is null)
        {
            return ChildAgeEligibilityEvaluation.RuleNotConfigured();
        }

        var referenceDate = ResolveReferenceDate(applicable.ReferenceDateMode, academicYear.StartDate);

        if (birthDate is null)
        {
            return ChildAgeEligibilityEvaluation.BirthDateRequired(applicable, referenceDate);
        }

        if (!CompletedCalendarMonths.TryCompute(birthDate.Value, referenceDate, out var months))
        {
            return ChildAgeEligibilityEvaluation.InvalidBirthDate(applicable, referenceDate);
        }

        if (months < applicable.MinAgeCompletedMonths)
        {
            return ChildAgeEligibilityEvaluation.FromRule(
                applicable,
                referenceDate,
                months,
                ChildAgeEligibilityResultCode.NotEligibleBelowMinimum,
                canContinue: false);
        }

        if (months > applicable.MaxAgeCompletedMonths)
        {
            return ChildAgeEligibilityEvaluation.FromRule(
                applicable,
                referenceDate,
                months,
                ChildAgeEligibilityResultCode.NotEligibleAboveMaximum,
                canContinue: false);
        }

        return ChildAgeEligibilityEvaluation.FromRule(
            applicable,
            referenceDate,
            months,
            ChildAgeEligibilityResultCode.Eligible,
            canContinue: true);
    }

    private static DateOnly ResolveReferenceDate(
        ChildAgeReferenceDateMode mode,
        DateOnly academicYearStartDate) =>
        mode switch
        {
            ChildAgeReferenceDateMode.AcademicYearStart => academicYearStartDate,
            _ => academicYearStartDate,
        };
}
