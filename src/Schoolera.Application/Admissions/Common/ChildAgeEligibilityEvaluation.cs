using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Live evaluation result for child age eligibility (Admission + reusable Transfer). </summary>
public sealed record ChildAgeEligibilityEvaluation(
    ChildAgeEligibilityResultCode ResultCode,
    int? CalculatedAgeCompletedMonths,
    int? MinAgeCompletedMonths,
    int? MaxAgeCompletedMonths,
    DateOnly? ReferenceDate,
    ChildAgeReferenceDateMode? ReferenceDateMode,
    Guid? RuleId,
    int? RuleVersion,
    string? ExplanationAr,
    string? ExplanationEn,
    bool ManualExceptionAllowed,
    bool CanContinue)
{
    public static ChildAgeEligibilityEvaluation RuleNotConfigured() =>
        new(
            ChildAgeEligibilityResultCode.RuleNotConfigured,
            CalculatedAgeCompletedMonths: null,
            MinAgeCompletedMonths: null,
            MaxAgeCompletedMonths: null,
            ReferenceDate: null,
            ReferenceDateMode: null,
            RuleId: null,
            RuleVersion: null,
            ExplanationAr: null,
            ExplanationEn: null,
            ManualExceptionAllowed: false,
            CanContinue: true);

    public static ChildAgeEligibilityEvaluation BirthDateRequired(
        Schoolera.Domain.Entities.SchoolChildAgeEligibilityRule rule,
        DateOnly referenceDate) =>
        FromRule(rule, referenceDate, null, ChildAgeEligibilityResultCode.BirthDateRequired, canContinue: false);

    public static ChildAgeEligibilityEvaluation InvalidBirthDate(
        Schoolera.Domain.Entities.SchoolChildAgeEligibilityRule rule,
        DateOnly referenceDate) =>
        FromRule(rule, referenceDate, null, ChildAgeEligibilityResultCode.InvalidBirthDate, canContinue: false);

    public static ChildAgeEligibilityEvaluation FromRule(
        Schoolera.Domain.Entities.SchoolChildAgeEligibilityRule rule,
        DateOnly referenceDate,
        int? calculatedMonths,
        ChildAgeEligibilityResultCode resultCode,
        bool canContinue) =>
        new(
            resultCode,
            calculatedMonths,
            rule.MinAgeCompletedMonths,
            rule.MaxAgeCompletedMonths,
            referenceDate,
            rule.ReferenceDateMode,
            rule.Id,
            rule.RuleVersion,
            rule.ExplanationAr,
            rule.ExplanationEn,
            rule.ManualExceptionAllowed,
            canContinue);
}
