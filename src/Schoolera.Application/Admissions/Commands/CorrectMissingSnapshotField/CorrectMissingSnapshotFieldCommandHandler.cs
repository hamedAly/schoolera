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

namespace Schoolera.Application.Admissions.Commands.CorrectMissingSnapshotField;

public sealed record CorrectMissingSnapshotFieldCommand(
    Guid ApplicationId,
    CorrectMissingSnapshotFieldRequest Body)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class CorrectMissingSnapshotFieldCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    ILogger<CorrectMissingSnapshotFieldCommandHandler> logger)
    : IRequestHandler<CorrectMissingSnapshotFieldCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        CorrectMissingSnapshotFieldCommand request,
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

        if (!AdmissionTransitionPolicy.CanParentEditRequestedItems(application.Status))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Editing is not allowed.",
                AdmissionErrorCodes.ReadOnly);
        }

        var activeRequest = application.ActiveMissingItemsRequest;
        var item = activeRequest?.Items.FirstOrDefault(entry => entry.Id == request.Body.MissingItemId);
        if (item is null)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Missing item was not requested.",
                AdmissionErrorCodes.MissingItemNotRequested);
        }

        if (item.Kind == AdmissionMissingItemKind.ParentSnapshotField &&
            item.ParentSnapshotField is not null)
        {
            application.ApplyMissingParentSnapshotCorrection(
                item.ParentSnapshotField.Value,
                request.Body.TextValue);
        }
        else if (item.Kind == AdmissionMissingItemKind.ChildSnapshotField &&
                 item.ChildSnapshotField is not null)
        {
            application.ApplyMissingChildSnapshotCorrection(
                item.ChildSnapshotField.Value,
                request.Body.TextValue,
                request.Body.BooleanValue);
        }
        else
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Use attachment or answer endpoints for this missing item.",
                AdmissionErrorCodes.MissingItemNotRequested);
        }

        item.MarkCompleted();
        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.MissingItemCorrected,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
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
            "Corrected missing snapshot field {MissingItemId} on application {ApplicationId}.",
            item.Id,
            loaded.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(loaded, identityProtector));
    }
}
