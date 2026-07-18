using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.ResubmitMissingDocuments;

public sealed record ResubmitMissingDocumentsCommand(
    Guid ApplicationId,
    ResubmitMissingDocumentsRequest Body)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class ResubmitMissingDocumentsCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IParentAccountService parentAccountService,
    ISchoolPortalRepository schoolPortalRepository,
    ILogger<ResubmitMissingDocumentsCommandHandler> logger)
    : IRequestHandler<ResubmitMissingDocumentsCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        ResubmitMissingDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Authentication required.",
                AdmissionErrorCodes.Forbidden);
        }

        var userId = currentUser.UserId.Value;
        var application = await admissionRepository.GetOwnedForUpdateAsync(
            userId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.NotFound<AdmissionApplicationDetailDto>();
        }

        if (AdmissionResults.HasRowVersionMismatch(request.Body.RowVersion, application.RowVersion))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ConcurrentUpdate);
        }

        if (!AdmissionTransitionPolicy.TryValidateParentTransition(
                application.Status,
                AdmissionApplicationStatus.UnderReview,
                application.ReviewStartedAtUtc,
                out var transitionCode))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Invalid status transition.",
                transitionCode);
        }

        var activeRequest = application.ActiveMissingItemsRequest;
        if (activeRequest is null)
        {
            // Idempotent: already cleared / back in review path.
            if (application.Status == AdmissionApplicationStatus.UnderReview)
            {
                var already = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
                    ?? application;
                return Result<AdmissionApplicationDetailDto>.Success(
                    AdmissionMapping.ToDetail(already, identityProtector));
            }

            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "No active missing-items request.",
                AdmissionErrorCodes.MissingItemsInvalid);
        }

        var incomplete = activeRequest.Items
            .Where(item => item.IsMandatory && !item.IsCompleted)
            .ToArray();
        if (incomplete.Length > 0)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Complete all mandatory missing items before resubmitting.",
                AdmissionErrorCodes.MissingItemsIncomplete);
        }

        var from = application.Status;
        activeRequest.Clear(userId);
        application.ResubmitMissingDocuments();
        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                from,
                application.Status,
                AdmissionHistoryActions.MissingDocumentsResubmitted,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: null));

        await AdmissionParentNotificationSupport.EnqueueAsync(
            notificationOutboxPublisher,
            parentAccountService,
            schoolPortalRepository,
            application,
            NotificationEventType.MissingInformationResubmitted,
            "missing-documents-resubmitted",
            cancellationToken);

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
            "Parent resubmitted missing documents for application {ApplicationId}.",
            loaded.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(loaded, identityProtector));
    }
}
