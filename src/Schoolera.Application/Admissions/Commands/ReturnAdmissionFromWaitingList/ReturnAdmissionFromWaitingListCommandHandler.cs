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

namespace Schoolera.Application.Admissions.Commands.ReturnAdmissionFromWaitingList;

public sealed record ReturnAdmissionFromWaitingListCommand(
    Guid SchoolId, Guid ApplicationId, ReturnFromWaitingListRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class ReturnAdmissionFromWaitingListCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    ILogger<ReturnAdmissionFromWaitingListCommandHandler> logger,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ReturnAdmissionFromWaitingListCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        ReturnAdmissionFromWaitingListCommand request,
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
                application.Status, AdmissionApplicationStatus.UnderReview, out var code) ||
            application.Status != AdmissionApplicationStatus.WaitingList)
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid status transition.", code);
        }

        var from = application.Status;
        application.ReturnToUnderReview();
        var role = access.HistoryActorRole;
        admissionRepository.AddHistory(new AdmissionApplicationHistory(
            application.Id, from, application.Status, AdmissionHistoryActions.ReturnedFromWaitingList,
            access.UserId, role, parentVisible: true, parentVisibleNote: null,
            internalNote: request.Body.InternalReviewNote?.Trim()));

        logger.LogInformation("Returned application {ApplicationId} from waiting list.", application.Id);
        return await SchoolLifecycleCommandSupport.FinishAsync(
            admissionRepository, userDirectory, unitOfWork, identityProtector,
            request.SchoolId, application, cancellationToken);
    }
}
