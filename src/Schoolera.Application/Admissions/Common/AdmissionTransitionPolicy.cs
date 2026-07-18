using Schoolera.Application.Admissions.Constants;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

/// <summary>
/// Central admission status transition policy. Controllers and Angular must not duplicate these rules.
/// ContractSent/Paid are not in <see cref="AdmissionApplicationStatus"/> and cannot be targeted.
/// </summary>
public static class AdmissionTransitionPolicy
{
    public static bool CanParentEdit(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.Draft;

    public static bool CanParentSubmit(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.Draft;

    public static bool CanParentUploadAttachments(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.Draft or AdmissionApplicationStatus.MissingDocuments;

    public static bool CanParentRemoveAttachments(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.Draft or AdmissionApplicationStatus.MissingDocuments;

    public static bool CanParentCancel(
        AdmissionApplicationStatus status,
        DateTimeOffset? reviewStartedAtUtc)
    {
        if (status == AdmissionApplicationStatus.Draft)
        {
            return true;
        }

        // Existing Phase-1 policy: Submitted only before review has started.
        if (status == AdmissionApplicationStatus.Submitted && reviewStartedAtUtc is null)
        {
            return true;
        }

        // UnderReview / MissingDocuments / Interview / Assessment / WaitingList:
        // listed as Parent→Cancelled only when this policy allows — currently it does not.
        return false;
    }

    public static bool CanParentResubmitMissingItems(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.MissingDocuments;

    public static bool CanParentEditRequestedItems(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.MissingDocuments;

    public static bool IsActiveDuplicateStatus(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.Draft
            or AdmissionApplicationStatus.Submitted
            or AdmissionApplicationStatus.UnderReview
            or AdmissionApplicationStatus.Accepted
            or AdmissionApplicationStatus.MissingDocuments
            or AdmissionApplicationStatus.InterviewRequired
            or AdmissionApplicationStatus.AssessmentRequired
            or AdmissionApplicationStatus.WaitingList
            or AdmissionApplicationStatus.Registered;

    /// <summary>Rejected permits a new application in Phase 1 (same child/school/branch/grade/year).</summary>
    public static bool PermitsReplacementAfter(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.Cancelled or AdmissionApplicationStatus.Rejected;

    public static bool CanSchoolStartReview(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.Submitted;

    public static bool CanSchoolRequestMissingItems(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.UnderReview;

    public static bool CanSchoolScheduleInterview(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.UnderReview;

    public static bool CanSchoolScheduleAssessment(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.UnderReview;

    public static bool CanSchoolMoveToWaitingList(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.UnderReview
            or AdmissionApplicationStatus.InterviewRequired
            or AdmissionApplicationStatus.AssessmentRequired;

    public static bool CanSchoolAccept(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.UnderReview
            or AdmissionApplicationStatus.InterviewRequired
            or AdmissionApplicationStatus.AssessmentRequired
            or AdmissionApplicationStatus.WaitingList;

    public static bool CanSchoolReject(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.UnderReview
            or AdmissionApplicationStatus.InterviewRequired
            or AdmissionApplicationStatus.AssessmentRequired
            or AdmissionApplicationStatus.WaitingList;

    public static bool CanSchoolMarkRegistered(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.Accepted;

    public static bool CanSchoolReturnToUnderReview(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.InterviewRequired
            or AdmissionApplicationStatus.AssessmentRequired
            or AdmissionApplicationStatus.WaitingList;

    public static bool CanSchoolCompleteInterview(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.InterviewRequired;

    public static bool CanSchoolCompleteAssessment(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.AssessmentRequired;

    public static bool CanSchoolRescheduleInterview(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.InterviewRequired;

    public static bool CanSchoolRescheduleAssessment(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.AssessmentRequired;

    public static bool CanSchoolCancelInterviewAppointment(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.InterviewRequired;

    public static bool CanSchoolCancelAssessmentAppointment(AdmissionApplicationStatus status) =>
        status == AdmissionApplicationStatus.AssessmentRequired;

    /// <summary>
    /// Manual age exception may be granted while the application is still editable/reviewable
    /// (Draft, Submitted, MissingDocuments, UnderReview).
    /// </summary>
    public static bool CanSchoolGrantAgeException(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.Draft
            or AdmissionApplicationStatus.Submitted
            or AdmissionApplicationStatus.MissingDocuments
            or AdmissionApplicationStatus.UnderReview;

    public static bool TryValidateParentTransition(
        AdmissionApplicationStatus from,
        AdmissionApplicationStatus to,
        DateTimeOffset? reviewStartedAtUtc,
        out string errorCode)
    {
        errorCode = AdmissionErrorCodes.InvalidTransition;

        if (from == AdmissionApplicationStatus.Draft && to == AdmissionApplicationStatus.Submitted)
        {
            return true;
        }

        if (from == AdmissionApplicationStatus.Draft && to == AdmissionApplicationStatus.Cancelled)
        {
            return true;
        }

        if (from == AdmissionApplicationStatus.Submitted && to == AdmissionApplicationStatus.Cancelled)
        {
            if (reviewStartedAtUtc is not null)
            {
                errorCode = AdmissionErrorCodes.CancellationNotAllowed;
                return false;
            }

            return true;
        }

        if (from == AdmissionApplicationStatus.MissingDocuments &&
            to == AdmissionApplicationStatus.UnderReview)
        {
            return true;
        }

        // Explicit Parent→Cancelled from later statuses only when CanParentCancel allows (currently never).
        if (to == AdmissionApplicationStatus.Cancelled &&
            from is AdmissionApplicationStatus.UnderReview
                or AdmissionApplicationStatus.InterviewRequired
                or AdmissionApplicationStatus.AssessmentRequired
                or AdmissionApplicationStatus.WaitingList)
        {
            if (!CanParentCancel(from, reviewStartedAtUtc))
            {
                errorCode = AdmissionErrorCodes.CancellationNotAllowed;
                return false;
            }

            return true;
        }

        if (to == from)
        {
            return false;
        }

        return false;
    }

    public static bool TryValidateSchoolTransition(
        AdmissionApplicationStatus from,
        AdmissionApplicationStatus to,
        out string errorCode)
    {
        errorCode = AdmissionErrorCodes.ReviewInvalidTransition;

        if (from == to)
        {
            return false;
        }

        return (from, to) switch
        {
            (AdmissionApplicationStatus.Submitted, AdmissionApplicationStatus.UnderReview) => true,

            (AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.MissingDocuments) => true,
            (AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.InterviewRequired) => true,
            (AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.AssessmentRequired) => true,
            (AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.WaitingList) => true,
            (AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.Accepted) => true,
            (AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.Rejected) => true,

            (AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.UnderReview) => true,
            (AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.WaitingList) => true,
            (AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.Accepted) => true,
            (AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.Rejected) => true,

            (AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.UnderReview) => true,
            (AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.WaitingList) => true,
            (AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.Accepted) => true,
            (AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.Rejected) => true,

            (AdmissionApplicationStatus.WaitingList, AdmissionApplicationStatus.UnderReview) => true,
            (AdmissionApplicationStatus.WaitingList, AdmissionApplicationStatus.Accepted) => true,
            (AdmissionApplicationStatus.WaitingList, AdmissionApplicationStatus.Rejected) => true,

            (AdmissionApplicationStatus.Accepted, AdmissionApplicationStatus.Registered) => true,

            _ => false,
        };
    }
}
