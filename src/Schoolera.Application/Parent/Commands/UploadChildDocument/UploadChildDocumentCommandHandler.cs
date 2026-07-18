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
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.UploadChildDocument;

public sealed record UploadChildDocumentCommand(
    Guid ChildId,
    ChildDocumentType DocumentType,
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long FileSize) : IRequest<Result<ChildDocumentDto>>
{
    public static UploadChildDocumentCommand FromRaw(
        Guid childId,
        int documentType,
        Stream content,
        string originalFileName,
        string contentType,
        long fileSize) =>
        new(
            childId,
            (ChildDocumentType)documentType,
            content,
            originalFileName,
            contentType,
            fileSize);
}

public sealed class UploadChildDocumentCommandHandler(
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    IChildDocumentRepository childDocumentRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UploadChildDocumentCommandHandler> logger)
    : IRequestHandler<UploadChildDocumentCommand, Result<ChildDocumentDto>>
{
    public async Task<Result<ChildDocumentDto>> Handle(
        UploadChildDocumentCommand request,
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

        var child = await childProfileRepository.GetOwnedAsync(userId, request.ChildId, cancellationToken);
        if (child is null || !child.IsActive)
        {
            return Result<ChildDocumentDto>.Failure(
                ["Child not found."],
                [ParentErrorCodes.ChildNotFound]);
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
                    Category = $"child-documents/{child.Id:N}",
                },
                cancellationToken);
        }
        catch (PrivateFileValidationException exception)
        {
            var code = ChildDocumentFileRules.MapPrivateFileError(exception);
            logger.LogWarning("Child document upload rejected: {Code}.", code);
            return Result<ChildDocumentDto>.Failure(
                [ChildDocumentFileRules.MessageFor(code)],
                [code]);
        }

        var document = new ChildDocument(
            child.Id,
            userId,
            request.DocumentType,
            ChildDocumentMapping.SafeOriginalFileName(request.OriginalFileName),
            stored.ContentType,
            stored.SizeBytes,
            stored.StoredFileReference,
            userId);

        await childDocumentRepository.AddAsync(document, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            await privateFileStorage.DeleteAsync(stored.StoredFileReference, cancellationToken);
            throw;
        }

        logger.LogInformation(
            "Uploaded child document {DocumentId} for child {ChildId}.",
            document.Id,
            child.Id);

        return Result<ChildDocumentDto>.Success(ChildDocumentMapping.ToDto(document));
    }
}
