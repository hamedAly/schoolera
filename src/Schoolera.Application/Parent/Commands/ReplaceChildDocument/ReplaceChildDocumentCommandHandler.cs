using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Common;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.ReplaceChildDocument;

public sealed record ReplaceChildDocumentCommand(
    Guid ChildId,
    Guid DocumentId,
    ChildDocumentType DocumentType,
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long FileSize) : IRequest<Result<ChildDocumentDto>>
{
    public static ReplaceChildDocumentCommand FromRaw(
        Guid childId,
        Guid documentId,
        int documentType,
        Stream content,
        string originalFileName,
        string contentType,
        long fileSize) =>
        new(
            childId,
            documentId,
            (ChildDocumentType)documentType,
            content,
            originalFileName,
            contentType,
            fileSize);
}

public sealed class ReplaceChildDocumentCommandHandler(
    ICurrentUser currentUser,
    IChildDocumentRepository childDocumentRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<ReplaceChildDocumentCommandHandler> logger)
    : IRequestHandler<ReplaceChildDocumentCommand, Result<ChildDocumentDto>>
{
    public async Task<Result<ChildDocumentDto>> Handle(
        ReplaceChildDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileSize <= 0)
        {
            return Result<ChildDocumentDto>.Failure(
                ["Document type or format is invalid."],
                [ParentErrorCodes.DocumentInvalidFile]);
        }

        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ChildDocumentDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var document = await childDocumentRepository.GetOwnedForUpdateAsync(
            userId,
            request.ChildId,
            request.DocumentId,
            cancellationToken);
        if (document is null)
        {
            return Result<ChildDocumentDto>.Failure(
                ["Document not found."],
                [ParentErrorCodes.DocumentNotFound]);
        }

        var precheck = ChildDocumentFileRules.ValidateBeforeSave(
            request.DocumentType,
            request.OriginalFileName,
            request.ContentType,
            request.FileSize,
            privateFileStorage.Policy);
        if (precheck is not null)
        {
            return Result<ChildDocumentDto>.Failure(
                [ChildDocumentFileRules.MessageFor(precheck)],
                [precheck]);
        }

        StoredPrivateFile stored;
        try
        {
            stored = await privateFileStorage.SaveAsync(
                new PrivateFileStoreRequest
                {
                    Content = request.Content,
                    OriginalFileName = request.OriginalFileName,
                    ContentType = request.ContentType,
                    DeclaredSizeBytes = request.FileSize,
                    Category = $"child-documents/{request.ChildId:N}",
                },
                cancellationToken);
        }
        catch (PrivateFileValidationException exception)
        {
            var code = ChildDocumentFileRules.MapPrivateFileError(exception);
            logger.LogWarning("Child document replace rejected: {Code}.", code);
            return Result<ChildDocumentDto>.Failure(
                [ChildDocumentFileRules.MessageFor(code)],
                [code]);
        }

        var previousKey = document.StorageKey;
        document.ReplaceFile(
            request.DocumentType,
            ChildDocumentMapping.SafeOriginalFileName(request.OriginalFileName),
            stored.ContentType,
            stored.SizeBytes,
            stored.StoredFileReference,
            userId);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            await privateFileStorage.DeleteAsync(stored.StoredFileReference, cancellationToken);
            throw;
        }

        await privateFileStorage.DeleteAsync(previousKey, cancellationToken);

        logger.LogInformation(
            "Replaced child document {DocumentId} for child {ChildId}.",
            document.Id,
            request.ChildId);

        return Result<ChildDocumentDto>.Success(ChildDocumentMapping.ToDto(document));
    }
}
