using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Resolves the single applicable published interview/assessment policy for a selection.</summary>
public static class InterviewAssessmentPolicyCatalog
{
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 480;
    public const int MinLeadTimeHours = 0;
    public const int MaxLeadTimeHours = 720;
    public const int MinBookingWindowDays = 0;
    public const int MaxBookingWindowDays = 365;
    public const int MinMaxRescheduleAttempts = 0;
    public const int MaxMaxRescheduleAttempts = 20;

    public static bool MatchesScope(
        SchoolInterviewAssessmentPolicy policy,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId) =>
        AdmissionScope.MatchesScope(
            policy.SchoolBranchId,
            policy.EducationalStageId,
            policy.GradeId,
            policy.AcademicYearId,
            branchId,
            stageId,
            gradeId,
            academicYearId);

    /// <summary>
    /// Unlike requirements (many codes), exactly one policy applies: most specific published active match, or null.
    /// </summary>
    public static SchoolInterviewAssessmentPolicy? ResolveApplicable(
        IEnumerable<SchoolInterviewAssessmentPolicy> candidates,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId) =>
        candidates
            .Where(policy =>
                policy.IsActive &&
                policy.PublicationStatus == InterviewAssessmentPolicyPublicationStatus.Published &&
                MatchesScope(policy, branchId, stageId, gradeId, academicYearId))
            .OrderByDescending(policy => policy.SpecificityScore)
            .ThenBy(policy => policy.Id)
            .FirstOrDefault();

    public static bool IsDurationInRange(int? minutes) =>
        minutes is >= MinDurationMinutes and <= MaxDurationMinutes;

    public static bool IsLeadTimeInRange(int? hours) =>
        hours is >= MinLeadTimeHours and <= MaxLeadTimeHours;

    public static bool IsBookingWindowInRange(int? days) =>
        days is null or (>= MinBookingWindowDays and <= MaxBookingWindowDays);

    public static bool IsMaxRescheduleInRange(int attempts) =>
        attempts is >= MinMaxRescheduleAttempts and <= MaxMaxRescheduleAttempts;
}
