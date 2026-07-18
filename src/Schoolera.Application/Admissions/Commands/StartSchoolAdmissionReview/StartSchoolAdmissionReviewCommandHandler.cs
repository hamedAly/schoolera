using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.StartSchoolAdmissionReview;

public sealed record StartSchoolAdmissionReviewCommand(
    Guid SchoolId,
    Guid ApplicationId,
    StartSchoolAdmissionReviewRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class StartSchoolAdmissionReviewCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<StartSchoolAdmissionReviewCommandHandler> logger)
    : IRequestHandler<StartSchoolAdmissionReviewCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        StartSchoolAdmissionReviewCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolAdmissionApplicationDetailDto>.Failure(
                accessResult.Errors,
                accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolAdmissionApplicationDetailDto>(
            access, SchoolPortalPermission.ManageApplicationReview, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var application = await admissionRepository.GetForSchoolForUpdateAsync(
            request.SchoolId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.ReviewNotFound<SchoolAdmissionApplicationDetailDto>();
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<SchoolAdmissionApplicationDetailDto>(
            access, application.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        if (AdmissionResults.HasRowVersionMismatch(request.Body.RowVersion, application.RowVersion))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ReviewConcurrentUpdate);
        }

        if (!AdmissionTransitionPolicy.TryValidateSchoolTransition(
                application.Status,
                AdmissionApplicationStatus.UnderReview,
                out var transitionCode))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid status transition.",
                transitionCode);
        }

        var fromStatus = application.Status;
        var actorRole = access.HistoryActorRole;
        var internalNote = string.IsNullOrWhiteSpace(request.Body.InternalReviewNote)
            ? null
            : request.Body.InternalReviewNote.Trim();

        application.StartReview(internalNote);
        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus,
                AdmissionApplicationStatus.UnderReview,
                AdmissionHistoryActions.ReviewStarted,
                access.UserId,
                actorRole,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: internalNote));

        var conflict = await AdmissionResults.TrySaveAsync<SchoolAdmissionApplicationDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return AdmissionResults.RemapSchoolSaveConflict(conflict);
        }

        var loaded = await admissionRepository.GetForSchoolAsync(
            request.SchoolId,
            application.Id,
            cancellationToken) ?? application;
        var users = await userDirectory.GetUsersAsync([loaded.ParentUserId], cancellationToken);
        users.TryGetValue(loaded.ParentUserId, out var parentUser);

        logger.LogInformation(
            "Started review on admission application {ApplicationId} for school {SchoolId} by {UserId}.",
            loaded.Id,
            request.SchoolId,
            access.UserId);

        return Result<SchoolAdmissionApplicationDetailDto>.Success(
            SchoolAdmissionMapping.ToSchoolDetail(
                loaded,
                SchoolAdmissionMapping.ToParentContact(parentUser),
                identityProtector));
    }
}
