using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.CancelAdmissionApplication;

public sealed record CancelAdmissionApplicationCommand(
    Guid ApplicationId,
    CancelAdmissionApplicationRequest Body)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class CancelAdmissionApplicationCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<CancelAdmissionApplicationCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<CancelAdmissionApplicationCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        CancelAdmissionApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionApplicationDetailDto>(localizer);
        }

        var application = await admissionRepository.GetOwnedForUpdateAsync(
            userId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.NotFound<AdmissionApplicationDetailDto>();
        }

        if (!AdmissionTransitionPolicy.TryValidateParentTransition(
                application.Status,
                AdmissionApplicationStatus.Cancelled,
                application.ReviewStartedAtUtc,
                out var transitionCode))
        {
            var message = transitionCode == AdmissionErrorCodes.CancellationNotAllowed
                ? "Cancellation is not allowed after review has started."
                : "Invalid status transition.";
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(message, transitionCode);
        }

        if (!AdmissionTransitionPolicy.CanParentCancel(application.Status, application.ReviewStartedAtUtc))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Cancellation is not allowed for the current status.",
                AdmissionErrorCodes.CancellationNotAllowed);
        }

        var fromStatus = application.Status;
        application.Cancel(request.Body.Reason);
        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus,
                AdmissionApplicationStatus.Cancelled,
                AdmissionHistoryActions.Cancelled,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: string.IsNullOrWhiteSpace(request.Body.Reason)
                    ? null
                    : request.Body.Reason.Trim(),
                internalNote: null));

        var conflict = await AdmissionResults.TrySaveAsync<AdmissionApplicationDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        logger.LogInformation(
            "Cancelled admission application {ApplicationId} for parent {UserId}.",
            loaded.Id,
            userId);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(loaded, identityProtector));
    }
}
