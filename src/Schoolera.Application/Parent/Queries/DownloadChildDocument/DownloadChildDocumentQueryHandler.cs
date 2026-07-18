using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Common;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Parent.Queries.DownloadChildDocument;

public sealed record DownloadChildDocumentQuery(Guid ChildId, Guid DocumentId)
    : IRequest<Result<AdmissionAttachmentDownloadDto>>;

public sealed class DownloadChildDocumentQueryValidator : AbstractValidator<DownloadChildDocumentQuery>
{
    public DownloadChildDocumentQueryValidator()
    {
        RuleFor(query => query.ChildId).NotEmpty();
        RuleFor(query => query.DocumentId).NotEmpty();
    }
}

public sealed class DownloadChildDocumentQueryHandler(
    ICurrentUser currentUser,
    IChildDocumentRepository childDocumentRepository,
    IPrivateFileStorage privateFileStorage,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<DownloadChildDocumentQueryHandler> logger)
    : IRequestHandler<DownloadChildDocumentQuery, Result<AdmissionAttachmentDownloadDto>>
{
    public async Task<Result<AdmissionAttachmentDownloadDto>> Handle(
        DownloadChildDocumentQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<AdmissionAttachmentDownloadDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var document = await childDocumentRepository.GetOwnedAsync(
            userId,
            request.ChildId,
            request.DocumentId,
            cancellationToken);
        if (document is null)
        {
            return Result<AdmissionAttachmentDownloadDto>.Failure(
                ["Document not found."],
                [ParentErrorCodes.DocumentNotFound]);
        }

        var stream = await privateFileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        if (stream is null)
        {
            logger.LogWarning("Stored file missing for child document {DocumentId}.", document.Id);
            return Result<AdmissionAttachmentDownloadDto>.Failure(
                ["Document not found."],
                [ParentErrorCodes.DocumentNotFound]);
        }

        return Result<AdmissionAttachmentDownloadDto>.Success(
            new AdmissionAttachmentDownloadDto(
                stream,
                document.ContentType,
                ChildDocumentMapping.SafeOriginalFileName(document.OriginalFileName),
                document.FileSizeBytes));
    }
}
