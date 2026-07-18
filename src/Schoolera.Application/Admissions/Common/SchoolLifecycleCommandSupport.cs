using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed record SchoolLifecycleBeginResult(
    Result<SchoolAdmissionApplicationDetailDto>? EarlyResult,
    SchoolPortalAccessContext? Access,
    AdmissionApplication? Application);

public static class SchoolLifecycleCommandSupport
{
    public static async Task<SchoolLifecycleBeginResult> BeginAsync(
        ISchoolPortalAccess portalAccess,
        IAdmissionApplicationRepository admissionRepository,
        IStringLocalizer<SchoolPortalMessages> localizer,
        Guid schoolId,
        Guid applicationId,
        byte[]? rowVersion,
        CancellationToken cancellationToken,
        SchoolPortalPermission permission = SchoolPortalPermission.ManageApplicationReview)
    {
        var accessResult = await portalAccess.ResolveAsync(schoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return new SchoolLifecycleBeginResult(
                Result<SchoolAdmissionApplicationDetailDto>.Failure(
                    accessResult.Errors,
                    accessResult.ErrorCodes),
                null,
                null);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolAdmissionApplicationDetailDto>(
            accessResult.Data, permission, localizer);
        if (!permissionCheck.Succeeded)
        {
            return new SchoolLifecycleBeginResult(permissionCheck, null, null);
        }

        var application = await admissionRepository.GetForSchoolForUpdateAsync(
            schoolId, applicationId, cancellationToken);
        if (application is null)
        {
            return new SchoolLifecycleBeginResult(
                AdmissionResults.ReviewNotFound<SchoolAdmissionApplicationDetailDto>(),
                null,
                null);
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<SchoolAdmissionApplicationDetailDto>(
            accessResult.Data, application.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return new SchoolLifecycleBeginResult(branchCheck, null, null);
        }

        if (AdmissionResults.HasRowVersionMismatch(rowVersion, application.RowVersion))
        {
            return new SchoolLifecycleBeginResult(
                AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "The application was modified by another operation.",
                    AdmissionErrorCodes.ReviewConcurrentUpdate),
                null,
                null);
        }

        return new SchoolLifecycleBeginResult(null, accessResult.Data, application);
    }

    public static async Task<Result<SchoolAdmissionApplicationDetailDto>> FinishAsync(
        IAdmissionApplicationRepository admissionRepository,
        IUserDirectory userDirectory,
        IUnitOfWork unitOfWork,
        IChildIdentityProtector identityProtector,
        Guid schoolId,
        AdmissionApplication application,
        CancellationToken cancellationToken)
    {
        var conflict = await AdmissionResults.TrySaveAsync<SchoolAdmissionApplicationDetailDto>(
            unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return AdmissionResults.RemapSchoolSaveConflict(conflict);
        }

        var loaded = await admissionRepository.GetForSchoolAsync(
            schoolId, application.Id, cancellationToken) ?? application;
        var users = await userDirectory.GetUsersAsync([loaded.ParentUserId], cancellationToken);
        users.TryGetValue(loaded.ParentUserId, out var parentUser);
        return Result<SchoolAdmissionApplicationDetailDto>.Success(
            SchoolAdmissionMapping.ToSchoolDetail(
                loaded,
                SchoolAdmissionMapping.ToParentContact(parentUser),
                identityProtector));
    }

    public static Result<SchoolAdmissionApplicationDetailDto>? ApplyPostAppointmentStatus(
        AdmissionApplication application,
        CompleteAdmissionAppointmentRequest body,
        out string? historyActionOverride)
    {
        historyActionOverride = null;
        var next = body.NextStatus;
        if (!AdmissionTransitionPolicy.TryValidateSchoolTransition(application.Status, next, out var code))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid status transition.",
                code);
        }

        switch (next)
        {
            case AdmissionApplicationStatus.UnderReview:
                application.ReturnToUnderReview();
                break;
            case AdmissionApplicationStatus.WaitingList:
                application.MoveToWaitingList(body.WaitingListReason, body.WaitingListPosition, body.WaitingListReviewDate);
                historyActionOverride = AdmissionHistoryActions.MovedToWaitingList;
                break;
            case AdmissionApplicationStatus.Accepted:
                application.Accept();
                break;
            case AdmissionApplicationStatus.Rejected:
                if (string.IsNullOrWhiteSpace(body.ParentVisibleRejectionReason))
                {
                    return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                        "Rejection reason is required.",
                        AdmissionErrorCodes.ReviewRejectionReasonRequired);
                }

                application.Reject(body.ParentVisibleRejectionReason, null);
                break;
            default:
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Invalid next status after appointment completion.",
                    AdmissionErrorCodes.ReviewInvalidTransition);
        }

        return null;
    }
}
