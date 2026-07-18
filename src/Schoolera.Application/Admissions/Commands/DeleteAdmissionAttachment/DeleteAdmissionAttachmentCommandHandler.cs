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
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.DeleteAdmissionAttachment;

public sealed record DeleteAdmissionAttachmentCommand(Guid ApplicationId, Guid AttachmentId)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class DeleteAdmissionAttachmentCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<DeleteAdmissionAttachmentCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<DeleteAdmissionAttachmentCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        DeleteAdmissionAttachmentCommand request,
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

        if (!AdmissionTransitionPolicy.CanParentRemoveAttachments(application.Status))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Attachments cannot be removed for the current status.",
                AdmissionErrorCodes.AttachmentReadOnly);
        }

        var attachment = await admissionRepository.GetOwnedAttachmentForUpdateAsync(
            userId,
            request.ApplicationId,
            request.AttachmentId,
            cancellationToken);
        if (attachment is null)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Attachment not found.",
                AdmissionErrorCodes.AttachmentNotFound);
        }

        var storageKey = attachment.StorageKey;
        admissionRepository.RemoveAttachment(attachment);
        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.AttachmentRemoved,
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

        await privateFileStorage.DeleteAsync(storageKey, cancellationToken);

        var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        logger.LogInformation(
            "Deleted admission attachment {AttachmentId} for application {ApplicationId}.",
            request.AttachmentId,
            application.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(loaded, identityProtector));
    }
}
