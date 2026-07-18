using FluentValidation;
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

namespace Schoolera.Application.Admissions.Queries.DownloadAdmissionAttachment;

public sealed record DownloadAdmissionAttachmentQuery(Guid ApplicationId, Guid AttachmentId)
    : IRequest<Result<AdmissionAttachmentDownloadDto>>;

public sealed class DownloadAdmissionAttachmentQueryValidator
    : AbstractValidator<DownloadAdmissionAttachmentQuery>
{
    public DownloadAdmissionAttachmentQueryValidator()
    {
        RuleFor(query => query.ApplicationId).NotEmpty();
        RuleFor(query => query.AttachmentId).NotEmpty();
    }
}

public sealed class DownloadAdmissionAttachmentQueryHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IPrivateFileStorage privateFileStorage,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<DownloadAdmissionAttachmentQueryHandler> logger)
    : IRequestHandler<DownloadAdmissionAttachmentQuery, Result<AdmissionAttachmentDownloadDto>>
{
    public async Task<Result<AdmissionAttachmentDownloadDto>> Handle(
        DownloadAdmissionAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionAttachmentDownloadDto>(localizer);
        }

        var attachment = await admissionRepository.GetOwnedAttachmentAsync(
            userId,
            request.ApplicationId,
            request.AttachmentId,
            cancellationToken);

        // Same safe 404 for missing applications/attachments and non-owned rows.
        if (attachment is null)
        {
            return AdmissionResults.Failure<AdmissionAttachmentDownloadDto>(
                "Attachment not found.",
                AdmissionErrorCodes.AttachmentNotFound);
        }

        var stream = await privateFileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        if (stream is null)
        {
            logger.LogWarning(
                "Stored file missing for admission attachment {AttachmentId}.",
                attachment.Id);
            return AdmissionResults.Failure<AdmissionAttachmentDownloadDto>(
                "Attachment not found.",
                AdmissionErrorCodes.AttachmentNotFound);
        }

        return Result<AdmissionAttachmentDownloadDto>.Success(
            new AdmissionAttachmentDownloadDto(
                stream,
                attachment.ContentType,
                AdmissionMapping.SafeOriginalFileName(attachment.OriginalFileName),
                attachment.FileSizeBytes));
    }
}
