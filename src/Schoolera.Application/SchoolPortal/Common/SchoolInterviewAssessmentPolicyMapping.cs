using Schoolera.Application.Admissions.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Common;

internal static class SchoolInterviewAssessmentPolicyMapping
{
    public static SchoolInterviewAssessmentPolicyListItemDto ToListItem(SchoolInterviewAssessmentPolicy policy) =>
        new(
            policy.Id,
            policy.RequirementMode,
            policy.DeliveryMode,
            policy.PublicationStatus,
            policy.IsActive,
            policy.PolicyVersion,
            policy.SchoolBranchId,
            policy.EducationalStageId,
            policy.GradeId,
            policy.AcademicYearId,
            policy.SpecificityScore,
            policy.UpdatedAtUtc);

    public static SchoolInterviewAssessmentPolicyDetailDto ToDetail(SchoolInterviewAssessmentPolicy policy) =>
        new(
            policy.Id,
            policy.RequirementMode,
            policy.DeliveryMode,
            policy.RequiredParticipants,
            policy.ExpectedDurationMinutes,
            policy.BookingWindowOpensDaysBefore,
            policy.BookingWindowClosesDaysBefore,
            policy.MinimumSchedulingLeadTimeHours,
            policy.ParentReschedulingAllowed,
            policy.MaxParentRescheduleAttempts,
            policy.ParentCancellationAllowed,
            policy.PreparationNotesAr,
            policy.PreparationNotesEn,
            policy.OnSiteInstructionsAr,
            policy.OnSiteInstructionsEn,
            policy.OnlineInstructionsAr,
            policy.OnlineInstructionsEn,
            policy.MeetingProviderCode,
            policy.HybridSelectionAuthority,
            policy.PublicationStatus,
            policy.IsActive,
            policy.PolicyVersion,
            policy.SchoolBranchId,
            policy.EducationalStageId,
            policy.GradeId,
            policy.AcademicYearId,
            policy.ScopeKey,
            policy.SpecificityScore,
            policy.CreatedAtUtc,
            policy.UpdatedAtUtc,
            policy.PublishedAtUtc,
            policy.RowVersion ?? Array.Empty<byte>());

    public static SafeInterviewAssessmentPolicySummaryDto ToSafeSummary(
        SchoolInterviewAssessmentPolicy policy,
        bool preferArabic,
        bool isOnlineCapabilityAvailable) =>
        new(
            policy.RequirementMode,
            policy.DeliveryMode,
            policy.RequiredParticipants,
            policy.ExpectedDurationMinutes,
            policy.BookingWindowOpensDaysBefore,
            policy.BookingWindowClosesDaysBefore,
            policy.MinimumSchedulingLeadTimeHours,
            policy.ParentReschedulingAllowed,
            policy.MaxParentRescheduleAttempts,
            policy.ParentCancellationAllowed,
            Pick(preferArabic, policy.PreparationNotesAr, policy.PreparationNotesEn),
            Pick(preferArabic, policy.OnSiteInstructionsAr, policy.OnSiteInstructionsEn),
            Pick(preferArabic, policy.OnlineInstructionsAr, policy.OnlineInstructionsEn),
            policy.MeetingProviderCode,
            policy.HybridSelectionAuthority,
            policy.PolicyVersion,
            isOnlineCapabilityAvailable);

    public static SafeInterviewAssessmentPolicySummaryDto ToSafeSummary(
        AdmissionApplicationInterviewAssessmentPolicySnapshot snapshot,
        bool preferArabic,
        bool isOnlineCapabilityAvailable) =>
        new(
            snapshot.RequirementMode,
            snapshot.DeliveryMode,
            snapshot.RequiredParticipants,
            snapshot.ExpectedDurationMinutes,
            snapshot.BookingWindowOpensDaysBefore,
            snapshot.BookingWindowClosesDaysBefore,
            snapshot.MinimumSchedulingLeadTimeHours,
            snapshot.ParentReschedulingAllowed,
            snapshot.MaxParentRescheduleAttempts,
            snapshot.ParentCancellationAllowed,
            Pick(preferArabic, snapshot.PreparationNotesAr, snapshot.PreparationNotesEn),
            Pick(preferArabic, snapshot.OnSiteInstructionsAr, snapshot.OnSiteInstructionsEn),
            Pick(preferArabic, snapshot.OnlineInstructionsAr, snapshot.OnlineInstructionsEn),
            snapshot.MeetingProviderCode,
            snapshot.HybridSelectionAuthority,
            snapshot.PolicyVersion,
            isOnlineCapabilityAvailable);

    public static bool NeedsOnlineCapability(InterviewAssessmentDeliveryMode? deliveryMode) =>
        deliveryMode is InterviewAssessmentDeliveryMode.Online or InterviewAssessmentDeliveryMode.Hybrid;

    /// <summary>
    /// Returns a stable SchoolPortal error code when the policy cannot be published; otherwise null.
    /// </summary>
    public static string? ValidateForPublish(
        SchoolInterviewAssessmentPolicy policy,
        SchoolBranch? branch,
        bool hasActiveMeetingProvider,
        IReadOnlySet<string> meetingProviderCodes)
    {
        if (policy.SpecificityScore <= 0)
        {
            return SchoolPortalErrorCodes.InterviewAssessmentPolicyInvalidScope;
        }

        if (policy.RequirementMode == InterviewAssessmentRequirementMode.NotRequired)
        {
            return null;
        }

        if (policy.RequirementMode is not (
            InterviewAssessmentRequirementMode.InterviewOnly or
            InterviewAssessmentRequirementMode.AssessmentOnly or
            InterviewAssessmentRequirementMode.InterviewAndAssessment))
        {
            return SchoolPortalErrorCodes.InterviewAssessmentPolicyInvalidMode;
        }

        if (policy.DeliveryMode is null ||
            !Enum.IsDefined(policy.DeliveryMode.Value) ||
            policy.RequiredParticipants is null ||
            !Enum.IsDefined(policy.RequiredParticipants.Value))
        {
            return SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid;
        }

        if (!InterviewAssessmentPolicyCatalog.IsDurationInRange(policy.ExpectedDurationMinutes) ||
            !InterviewAssessmentPolicyCatalog.IsLeadTimeInRange(policy.MinimumSchedulingLeadTimeHours) ||
            !InterviewAssessmentPolicyCatalog.IsBookingWindowInRange(policy.BookingWindowOpensDaysBefore) ||
            !InterviewAssessmentPolicyCatalog.IsBookingWindowInRange(policy.BookingWindowClosesDaysBefore) ||
            !InterviewAssessmentPolicyCatalog.IsMaxRescheduleInRange(policy.MaxParentRescheduleAttempts))
        {
            return SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid;
        }

        if (string.IsNullOrWhiteSpace(policy.PreparationNotesAr) ||
            string.IsNullOrWhiteSpace(policy.PreparationNotesEn))
        {
            return SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid;
        }

        var delivery = policy.DeliveryMode.Value;
        if (delivery is InterviewAssessmentDeliveryMode.OnSite or InterviewAssessmentDeliveryMode.Hybrid)
        {
            if (policy.SchoolBranchId is null)
            {
                return SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid;
            }

            if (branch is null ||
                !branch.IsActive ||
                branch.SchoolId != policy.SchoolId ||
                (string.IsNullOrWhiteSpace(branch.AddressLineAr) &&
                 string.IsNullOrWhiteSpace(branch.AddressLineEn) &&
                 string.IsNullOrWhiteSpace(branch.StreetName)))
            {
                return SchoolPortalErrorCodes.InterviewAssessmentPolicyOnSiteBranchInvalid;
            }

            if (string.IsNullOrWhiteSpace(policy.OnSiteInstructionsAr) ||
                string.IsNullOrWhiteSpace(policy.OnSiteInstructionsEn))
            {
                return SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid;
            }
        }

        if (delivery is InterviewAssessmentDeliveryMode.Online or InterviewAssessmentDeliveryMode.Hybrid)
        {
            if (string.IsNullOrWhiteSpace(policy.MeetingProviderCode) ||
                !hasActiveMeetingProvider ||
                !meetingProviderCodes.Contains(policy.MeetingProviderCode))
            {
                return SchoolPortalErrorCodes.InterviewAssessmentPolicyMeetingUnavailable;
            }

            if (string.IsNullOrWhiteSpace(policy.OnlineInstructionsAr) ||
                string.IsNullOrWhiteSpace(policy.OnlineInstructionsEn))
            {
                return SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid;
            }
        }

        if (delivery == InterviewAssessmentDeliveryMode.Hybrid &&
            policy.HybridSelectionAuthority is null)
        {
            return SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid;
        }

        return null;
    }

    private static string? Pick(bool preferArabic, string? ar, string? en) =>
        preferArabic
            ? (ar ?? en)
            : (en ?? ar);
}
