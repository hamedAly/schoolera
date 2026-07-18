using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Common;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Resources;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.CopyChildDocumentToAdmissionDraft;

public sealed record CopyChildDocumentToAdmissionDraftCommand(
    Guid ApplicationId,
    Guid ChildDocumentId,
    Guid? RequirementSnapshotId = null) : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class CopyChildDocumentToAdmissionDraftCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionRequirementSnapshotService requirementSnapshotService,
    IChildDocumentRepository childDocumentRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<CopyChildDocumentToAdmissionDraftCommandHandler> logger)
    : IRequestHandler<CopyChildDocumentToAdmissionDraftCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        CopyChildDocumentToAdmissionDraftCommand request,
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

        if (!AdmissionTransitionPolicy.CanParentUploadAttachments(application.Status))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Attachments cannot be uploaded for the current status.",
                AdmissionErrorCodes.AttachmentReadOnly);
        }

        await requirementSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken);

        AdmissionApplicationRequirementSnapshot? requirementSnapshot = null;
        if (request.RequirementSnapshotId is { } snapshotId)
        {
            var detail = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken);
            requirementSnapshot = detail?.RequirementSnapshots
                .FirstOrDefault(snapshot => snapshot.Id == snapshotId);
            if (requirementSnapshot is null)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Requirement snapshot not found.",
                    AdmissionErrorCodes.RequirementSnapshotNotFound);
            }

            if (!requirementSnapshot.AllowChildVaultCopy)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Attachment type or format is invalid.",
                    AdmissionErrorCodes.RequirementDocumentRules);
            }
        }

        var existing = await childDocumentRepository.GetSourceLinkedAttachmentAsync(
            application.Id,
            request.ChildDocumentId,
            cancellationToken);
        if (existing is not null &&
            (requirementSnapshot is null || existing.RequirementSnapshotId == requirementSnapshot.Id))
        {
            var loadedExisting = await admissionRepository.GetOwnedAsync(
                userId,
                application.Id,
                cancellationToken) ?? application;
            return Result<AdmissionApplicationDetailDto>.Success(
                AdmissionMapping.ToDetail(loadedExisting, identityProtector));
        }

        var vaultDocument = await childDocumentRepository.GetOwnedAsync(
            userId,
            application.ChildProfileId,
            request.ChildDocumentId,
            cancellationToken);
        if (vaultDocument is null)
        {
            return Result<AdmissionApplicationDetailDto>.Failure(
                ["Document not found."],
                [ParentErrorCodes.DocumentNotFound]);
        }

        if (vaultDocument.ChildProfileId != application.ChildProfileId)
        {
            return Result<AdmissionApplicationDetailDto>.Failure(
                ["Document not found."],
                [ParentErrorCodes.DocumentNotFound]);
        }

        var documentCode = ChildDocumentMapping.ToAdmissionRequiredDocumentCode(vaultDocument.DocumentType);
        if (requirementSnapshot is not null)
        {
            if (requirementSnapshot.Kind != AdmissionRequirementKind.ApplicationDocument)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Attachment type or format is invalid.",
                    AdmissionErrorCodes.RequirementDocumentRules);
            }

            if (requirementSnapshot.DocumentCode is { } expected &&
                documentCode is { } actual &&
                expected != actual)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Attachment type or format is invalid.",
                    AdmissionErrorCodes.RequirementDocumentRules);
            }

            if (!ValidateFileForSnapshot(vaultDocument.OriginalFileName, vaultDocument.FileSizeBytes, requirementSnapshot))
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Attachment type or format is invalid.",
                    AdmissionErrorCodes.RequirementDocumentRules);
            }
        }

        await using var sourceStream = await privateFileStorage.OpenReadAsync(
            vaultDocument.StorageKey,
            cancellationToken);
        if (sourceStream is null)
        {
            logger.LogWarning(
                "Vault document {DocumentId} storage missing during admission copy.",
                vaultDocument.Id);
            return Result<AdmissionApplicationDetailDto>.Failure(
                [ChildDocumentFileRules.MessageFor(ParentErrorCodes.DocumentCopyFailed)],
                [ParentErrorCodes.DocumentCopyFailed]);
        }

        StoredPrivateFile stored;
        try
        {
            stored = await privateFileStorage.SaveAsync(
                new PrivateFileStoreRequest
                {
                    Content = sourceStream,
                    OriginalFileName = vaultDocument.OriginalFileName,
                    ContentType = vaultDocument.ContentType,
                    DeclaredSizeBytes = vaultDocument.FileSizeBytes,
                    Category = $"admission-applications/{application.Id:N}",
                },
                cancellationToken);
        }
        catch (PrivateFileValidationException exception)
        {
            logger.LogWarning(
                "Vault-to-admission copy rejected for document {DocumentId}: {Code}.",
                vaultDocument.Id,
                exception.ErrorCode);
            return Result<AdmissionApplicationDetailDto>.Failure(
                [ChildDocumentFileRules.MessageFor(ParentErrorCodes.DocumentCopyFailed)],
                [ParentErrorCodes.DocumentCopyFailed]);
        }

        AdmissionApplicationAttachment? replaced = null;
        if (requirementSnapshot is not null)
        {
            var detailForReplace = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken);
            var existingId = detailForReplace?.Attachments
                .FirstOrDefault(attachment => attachment.RequirementSnapshotId == requirementSnapshot.Id)
                ?.Id;
            if (existingId is { } id)
            {
                replaced = await admissionRepository.GetOwnedAttachmentForUpdateAsync(
                    userId,
                    application.Id,
                    id,
                    cancellationToken);
            }
        }

        if (replaced is not null)
        {
            var previousKey = replaced.StorageKey;
            replaced.ReplaceFile(
                ChildDocumentMapping.SafeOriginalFileName(vaultDocument.OriginalFileName),
                stored.ContentType,
                stored.SizeBytes,
                stored.StoredFileReference,
                userId);
            if (requirementSnapshot?.DocumentCode is { } linkedCode)
            {
                replaced.LinkRequirementSnapshot(requirementSnapshot.Id, linkedCode);
            }

            var replaceConflict = await AdmissionResults.TrySaveAsync<AdmissionApplicationDetailDto>(
                unitOfWork,
                cancellationToken);
            if (replaceConflict is not null)
            {
                await privateFileStorage.DeleteAsync(stored.StoredFileReference, cancellationToken);
                return replaceConflict;
            }

            await privateFileStorage.DeleteAsync(previousKey, cancellationToken);
        }
        else
        {
            var attachment = new AdmissionApplicationAttachment(
                application.Id,
                ChildDocumentMapping.ToAdmissionAttachmentType(vaultDocument.DocumentType),
                ChildDocumentMapping.SafeOriginalFileName(vaultDocument.OriginalFileName),
                stored.ContentType,
                stored.SizeBytes,
                stored.StoredFileReference,
                userId,
                vaultDocument.Id,
                requirementSnapshot?.Id,
                documentCode ?? requirementSnapshot?.DocumentCode);

            admissionRepository.AddAttachment(attachment);
            var conflict = await AdmissionResults.TrySaveAsync<AdmissionApplicationDetailDto>(
                unitOfWork,
                cancellationToken);
            if (conflict is not null)
            {
                await privateFileStorage.DeleteAsync(stored.StoredFileReference, cancellationToken);
                return conflict;
            }
        }

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.AttachmentUploaded,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: null));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        logger.LogInformation(
            "Copied vault document {DocumentId} to application {ApplicationId}.",
            vaultDocument.Id,
            application.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(result, identityProtector));
    }

    private static bool ValidateFileForSnapshot(
        string originalFileName,
        long fileSize,
        AdmissionApplicationRequirementSnapshot snapshot)
    {
        var allowed = AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions);
        var extension = Path.GetExtension(originalFileName);
        if (allowed.Count > 0 &&
            !allowed.Contains(AdmissionRequirementCatalog.NormalizeExtension(extension)))
        {
            return false;
        }

        return snapshot.MaxFileSizeBytes is not { } max || fileSize <= max;
    }
}
