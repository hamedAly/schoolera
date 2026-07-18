using Microsoft.Extensions.Localization;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.MoveAdmissionToWaitingList;

public sealed record MoveAdmissionToWaitingListCommand(
    Guid SchoolId, Guid ApplicationId, MoveToWaitingListRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class MoveAdmissionToWaitingListCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IAdmissionEvaluationRepository evaluationRepository,
    IParentAccountService parentAccountService,
    ISchoolPortalRepository schoolPortalRepository,
    ILogger<MoveAdmissionToWaitingListCommandHandler> logger,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<MoveAdmissionToWaitingListCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        MoveAdmissionToWaitingListCommand request,
        CancellationToken cancellationToken)
    {
        var begin = await SchoolLifecycleCommandSupport.BeginAsync(
            portalAccess,
            admissionRepository,
            localizer,
            request.SchoolId,
            request.ApplicationId,
            request.Body.RowVersion, cancellationToken);
        if (begin.EarlyResult is not null)
        {
            return begin.EarlyResult;
        }

        var access = begin.Access!;
        var application = begin.Application!;

        if (!AdmissionTransitionPolicy.TryValidateSchoolTransition(
                application.Status, AdmissionApplicationStatus.WaitingList, out var code))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid status transition.", code);
        }

        if (!await evaluationRepository.HasFinalizedRequiredEvaluationAsync(
                application.Id, application.Status, cancellationToken))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "The required interview or assessment result has not been finalized.",
                AdmissionErrorCodes.EvaluationInvalidTransition);
        }

        var from = application.Status;
        application.MoveToWaitingList(
            request.Body.ParentVisibleReason,
            request.Body.Position,
            request.Body.ReviewDate);
        var role = access.HistoryActorRole;
        admissionRepository.AddHistory(new AdmissionApplicationHistory(
            application.Id, from, application.Status, AdmissionHistoryActions.MovedToWaitingList,
            access.UserId, role, parentVisible: true,
            parentVisibleNote: request.Body.ParentVisibleReason?.Trim(),
            internalNote: request.Body.InternalReviewNote?.Trim()));

        await AdmissionParentNotificationSupport.EnqueueAsync(
            notificationOutboxPublisher,
            parentAccountService,
            schoolPortalRepository,
            application,
            NotificationEventType.WaitingList,
            "waiting-list",
            cancellationToken);

        logger.LogInformation("Moved application {ApplicationId} to waiting list.", application.Id);
        return await SchoolLifecycleCommandSupport.FinishAsync(
            admissionRepository, userDirectory, unitOfWork, identityProtector,
            request.SchoolId, application, cancellationToken);
    }
}
