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

namespace Schoolera.Application.Admissions.Commands.MarkAdmissionRegistered;

public sealed record MarkAdmissionRegisteredCommand(
    Guid SchoolId, Guid ApplicationId, MarkRegisteredRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class MarkAdmissionRegisteredCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IParentAccountService parentAccountService,
    ISchoolPortalRepository schoolPortalRepository,
    ILogger<MarkAdmissionRegisteredCommandHandler> logger,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<MarkAdmissionRegisteredCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        MarkAdmissionRegisteredCommand request,
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

        // Idempotent: already registered.
        if (application.Status == AdmissionApplicationStatus.Registered &&
            application.RegisteredAtUtc is not null)
        {
            return await SchoolLifecycleCommandSupport.FinishAsync(
                admissionRepository, userDirectory, unitOfWork, identityProtector,
                request.SchoolId, application, cancellationToken);
        }

        if (!AdmissionTransitionPolicy.TryValidateSchoolTransition(
                application.Status, AdmissionApplicationStatus.Registered, out var code))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid status transition.", code);
        }

        var from = application.Status;
        application.MarkRegistered();
        var role = access.HistoryActorRole;
        admissionRepository.AddHistory(new AdmissionApplicationHistory(
            application.Id, from, application.Status, AdmissionHistoryActions.Registered,
            access.UserId, role, parentVisible: true, parentVisibleNote: null,
            internalNote: request.Body.InternalReviewNote?.Trim()));

        await AdmissionParentNotificationSupport.EnqueueAsync(
            notificationOutboxPublisher,
            parentAccountService,
            schoolPortalRepository,
            application,
            NotificationEventType.Registered,
            "registered",
            cancellationToken);

        logger.LogInformation("Marked application {ApplicationId} as registered.", application.Id);
        return await SchoolLifecycleCommandSupport.FinishAsync(
            admissionRepository, userDirectory, unitOfWork, identityProtector,
            request.SchoolId, application, cancellationToken);
    }
}
