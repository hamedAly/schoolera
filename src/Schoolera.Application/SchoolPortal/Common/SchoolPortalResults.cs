using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Exceptions;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Constants;

namespace Schoolera.Application.SchoolPortal.Common;

/// <summary>Builds localized <see cref="Result{T}"/> failures paired with stable school portal codes.</summary>
public static class SchoolPortalResults
{
    private static readonly IReadOnlyDictionary<string, string> CodeToMessageKey = new Dictionary<string, string>
    {
        [SchoolPortalErrorCodes.SchoolNotFound] = "SchoolNotFound",
        [SchoolPortalErrorCodes.AccessDenied] = "AccessDenied",
        [SchoolPortalErrorCodes.OwnerRequired] = "OwnerRequired",
        [SchoolPortalErrorCodes.NotEditable] = "NotEditable",
        [SchoolPortalErrorCodes.InvalidSchoolStatus] = "InvalidSchoolStatus",
        [SchoolPortalErrorCodes.ConcurrentUpdate] = "ConcurrentUpdate",
        [SchoolPortalErrorCodes.TeamMemberNotFound] = "TeamMemberNotFound",
        [SchoolPortalErrorCodes.TeamMemberAlreadyExists] = "TeamMemberAlreadyExists",
        [SchoolPortalErrorCodes.TeamUserNotEligible] = "TeamUserNotEligible",
        [SchoolPortalErrorCodes.CannotModifyOwner] = "CannotModifyOwner",
        [SchoolPortalErrorCodes.LastOwnerProtected] = "LastOwnerProtected",
        [SchoolPortalErrorCodes.InvalidTeamRole] = "InvalidTeamRole",
        [SchoolPortalErrorCodes.InvalidBranchScope] = "InvalidBranchScope",
        [SchoolPortalErrorCodes.BranchScopeDenied] = "BranchScopeDenied",
        [SchoolPortalErrorCodes.OwnershipTransferInvalid] = "OwnershipTransferInvalid",
        [SchoolPortalErrorCodes.CannotChangeStatus] = "CannotChangeStatus",
        [SchoolPortalErrorCodes.BranchNotFound] = "BranchNotFound",
        [SchoolPortalErrorCodes.OfferingNotFound] = "OfferingNotFound",
        [SchoolPortalErrorCodes.FeeNotFound] = "FeeNotFound",
        [SchoolPortalErrorCodes.InvalidFee] = "InvalidFee",
        [SchoolPortalErrorCodes.InvalidFeeDateRange] = "InvalidFeeDateRange",
        [SchoolPortalErrorCodes.InstallmentNotFound] = "InstallmentNotFound",
        [SchoolPortalErrorCodes.InvalidInstallment] = "InvalidInstallment",
        [SchoolPortalErrorCodes.DiscountNotFound] = "DiscountNotFound",
        [SchoolPortalErrorCodes.InvalidDiscount] = "InvalidDiscount",
        [SchoolPortalErrorCodes.FinancialNoteNotFound] = "FinancialNoteNotFound",
        [SchoolPortalErrorCodes.InvalidFeeVisibility] = "InvalidFeeVisibility",
        [SchoolPortalErrorCodes.ServiceNotFound] = "ServiceNotFound",
        [SchoolPortalErrorCodes.ImageNotFound] = "ImageNotFound",
        [SchoolPortalErrorCodes.DuplicateOffering] = "DuplicateOffering",
        [SchoolPortalErrorCodes.DuplicateFee] = "DuplicateFee",
        [SchoolPortalErrorCodes.CityDistrictMismatch] = "CityDistrictMismatch",
        [SchoolPortalErrorCodes.MainBranchRequired] = "MainBranchRequired",
        [SchoolPortalErrorCodes.InvalidMedia] = "InvalidMedia",
        [SchoolPortalErrorCodes.MediaLimitExceeded] = "MediaLimitExceeded",
        [SchoolPortalErrorCodes.FacilityNotFound] = "FacilityNotFound",
        [SchoolPortalErrorCodes.InvalidStage] = "InvalidStage",
        [SchoolPortalErrorCodes.InvalidGrade] = "InvalidGrade",
        [SchoolPortalErrorCodes.InvalidAcademicYear] = "InvalidAcademicYear",
        [SchoolPortalErrorCodes.DuplicateBranchSlug] = "DuplicateBranchSlug",
        [SchoolPortalErrorCodes.UserNotFound] = "UserNotFound",
        [SchoolPortalErrorCodes.InvalidReorder] = "InvalidReorder",
        [SchoolPortalErrorCodes.AdmissionRequirementNotFound] = "AdmissionRequirementNotFound",
        [SchoolPortalErrorCodes.AdmissionRequirementConflict] = "AdmissionRequirementConflict",
        [SchoolPortalErrorCodes.AdmissionRequirementInvalidScope] = "AdmissionRequirementInvalidScope",
        [SchoolPortalErrorCodes.AdmissionRequirementInvalidKind] = "AdmissionRequirementInvalidKind",
        [SchoolPortalErrorCodes.AdmissionRequirementInvalidField] = "AdmissionRequirementInvalidField",
        [SchoolPortalErrorCodes.AdmissionRequirementInvalidDocument] = "AdmissionRequirementInvalidDocument",
        [SchoolPortalErrorCodes.AdmissionRequirementInvalidFileTypes] = "AdmissionRequirementInvalidFileTypes",
        [SchoolPortalErrorCodes.AdmissionRequirementInvalidMaxSize] = "AdmissionRequirementInvalidMaxSize",
        [SchoolPortalErrorCodes.AdmissionRequirementPublishInvalid] = "AdmissionRequirementPublishInvalid",
        [SchoolPortalErrorCodes.AdmissionRequirementMustUnpublish] = "AdmissionRequirementMustUnpublish",
        [SchoolPortalErrorCodes.AdmissionQuestionNotFound] = "AdmissionQuestionNotFound",
        [SchoolPortalErrorCodes.AdmissionQuestionConflict] = "AdmissionQuestionConflict",
        [SchoolPortalErrorCodes.AdmissionQuestionInvalidScope] = "AdmissionQuestionInvalidScope",
        [SchoolPortalErrorCodes.AdmissionQuestionInvalidType] = "AdmissionQuestionInvalidType",
        [SchoolPortalErrorCodes.AdmissionQuestionInvalidOptions] = "AdmissionQuestionInvalidOptions",
        [SchoolPortalErrorCodes.AdmissionQuestionPublishInvalid] = "AdmissionQuestionPublishInvalid",
        [SchoolPortalErrorCodes.AdmissionQuestionMustUnpublish] = "AdmissionQuestionMustUnpublish",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyNotFound] = "InterviewAssessmentPolicyNotFound",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyConflict] = "InterviewAssessmentPolicyConflict",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyInvalidScope] = "InterviewAssessmentPolicyInvalidScope",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid] = "InterviewAssessmentPolicyPublishInvalid",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyMustUnpublish] = "InterviewAssessmentPolicyMustUnpublish",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyInvalidMode] = "InterviewAssessmentPolicyInvalidMode",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyMeetingUnavailable] = "InterviewAssessmentPolicyMeetingUnavailable",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyOnSiteBranchInvalid] = "InterviewAssessmentPolicyOnSiteBranchInvalid",
        [SchoolPortalErrorCodes.InterviewAssessmentPolicyReferenced] = "InterviewAssessmentPolicyReferenced",
        [SchoolPortalErrorCodes.InterviewFaqNotFound] = "InterviewFaqNotFound",
        [SchoolPortalErrorCodes.InterviewFaqInvalid] = "InterviewFaqInvalid",
        [SchoolPortalErrorCodes.InterviewFaqPublishInvalid] = "InterviewFaqPublishInvalid",
        [SchoolPortalErrorCodes.AgeEligibilityRuleNotFound] = "AgeEligibilityRuleNotFound",
        [SchoolPortalErrorCodes.AgeEligibilityRuleConflict] = "AgeEligibilityRuleConflict",
        [SchoolPortalErrorCodes.AgeEligibilityRuleInvalidScope] = "AgeEligibilityRuleInvalidScope",
        [SchoolPortalErrorCodes.AgeEligibilityRulePublishInvalid] = "AgeEligibilityRulePublishInvalid",
        [SchoolPortalErrorCodes.AgeEligibilityRuleMustUnpublish] = "AgeEligibilityRuleMustUnpublish",
        [SchoolPortalErrorCodes.AgeEligibilityRuleReferenced] = "AgeEligibilityRuleReferenced",
    };

    public static Result<T> Failure<T>(
        IStringLocalizer<SchoolPortalMessages> localizer,
        string messageKey,
        string errorCode) =>
        Result<T>.Failure([localizer[messageKey].Value], [errorCode]);

    public static Result<T> FailureForCode<T>(
        IStringLocalizer<SchoolPortalMessages> localizer,
        string errorCode) =>
        Result<T>.Failure([localizer[MessageKeyFor(errorCode)].Value], [errorCode]);

    public static async Task<Result<T>?> TrySaveAsync<T>(
        IUnitOfWork unitOfWork,
        IStringLocalizer<SchoolPortalMessages> localizer,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConcurrencyConflictException)
        {
            return Failure<T>(localizer, "ConcurrentUpdate", SchoolPortalErrorCodes.ConcurrentUpdate);
        }
    }

    private static string MessageKeyFor(string errorCode) =>
        CodeToMessageKey.TryGetValue(errorCode, out var key) ? key : "SchoolNotFound";
}
