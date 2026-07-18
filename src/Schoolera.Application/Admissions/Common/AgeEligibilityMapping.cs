using Schoolera.Application.Admissions.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

internal static class AgeEligibilityMapping
{
    public static AgeEligibilityResultDto? FromSnapshot(
        AdmissionApplicationChildAgeEligibilitySnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        return new AgeEligibilityResultDto(
            snapshot.ResultCode,
            snapshot.CalculatedAgeCompletedMonths,
            snapshot.MinAgeCompletedMonths,
            snapshot.MaxAgeCompletedMonths,
            snapshot.ReferenceDate,
            snapshot.ReferenceDateMode,
            snapshot.SourceRuleId,
            snapshot.RuleVersion,
            Explanation: null,
            snapshot.ManualExceptionAllowedAtEvaluation,
            snapshot.ManualExceptionIsApproved,
            snapshot.ManualExceptionReasonCode,
            CanContinue(snapshot.ResultCode),
            CanRequestAgeException: false);
    }

    public static AgeEligibilityResultDto FromEvaluation(
        ChildAgeEligibilityEvaluation evaluation,
        bool canRequestAgeException = false)
    {
        var preferArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        return new AgeEligibilityResultDto(
            evaluation.ResultCode,
            evaluation.CalculatedAgeCompletedMonths,
            evaluation.MinAgeCompletedMonths,
            evaluation.MaxAgeCompletedMonths,
            evaluation.ReferenceDate,
            evaluation.ReferenceDateMode,
            evaluation.RuleId,
            evaluation.RuleVersion,
            Prefer(preferArabic, evaluation.ExplanationAr, evaluation.ExplanationEn),
            evaluation.ManualExceptionAllowed,
            ManualExceptionIsApproved: evaluation.ResultCode ==
                ChildAgeEligibilityResultCode.ManualExceptionApproved,
            ManualExceptionReasonCode: null,
            evaluation.CanContinue,
            canRequestAgeException);
    }

    public static bool CanContinue(ChildAgeEligibilityResultCode code) =>
        code is ChildAgeEligibilityResultCode.Eligible
            or ChildAgeEligibilityResultCode.RuleNotConfigured
            or ChildAgeEligibilityResultCode.ManualExceptionApproved;

    public static bool ComputeCanGrantAgeException(
        bool hasManageApplicationReview,
        AdmissionApplicationStatus status,
        AgeEligibilityResultDto? ageEligibility) =>
        hasManageApplicationReview &&
        AdmissionTransitionPolicy.CanSchoolGrantAgeException(status) &&
        ageEligibility is not null &&
        ageEligibility.ManualExceptionAllowed &&
        !ageEligibility.ManualExceptionIsApproved &&
        ageEligibility.ResultCode is ChildAgeEligibilityResultCode.NotEligibleBelowMinimum
            or ChildAgeEligibilityResultCode.NotEligibleAboveMaximum;

    private static string? Prefer(bool preferArabic, string? ar, string? en) =>
        preferArabic
            ? (string.IsNullOrWhiteSpace(ar) ? en : ar)
            : (string.IsNullOrWhiteSpace(en) ? ar : en);
}
