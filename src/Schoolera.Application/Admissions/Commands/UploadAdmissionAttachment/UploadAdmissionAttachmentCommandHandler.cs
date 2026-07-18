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

namespace Schoolera.Application.Admissions.Commands.UploadAdmissionAttachment;

public sealed record UploadAdmissionAttachmentCommand(
    Guid ApplicationId,
    AdmissionAttachmentType AttachmentType,
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    Guid? RequirementSnapshotId = null,
    Guid? QuestionSnapshotId = null) : IRequest<Result<AdmissionApplicationDetailDto>>
{
    public static UploadAdmissionAttachmentCommand FromRaw(
        Guid applicationId,
        int attachmentType,
        Stream content,
        string originalFileName,
        string contentType,
        long fileSize,
        Guid? requirementSnapshotId = null,
        Guid? questionSnapshotId = null) =>
        new(
            applicationId,
            (AdmissionAttachmentType)attachmentType,
            content,
            originalFileName,
            contentType,
            fileSize,
            requirementSnapshotId,
            questionSnapshotId);
}

public sealed class UploadAdmissionAttachmentCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionRequirementSnapshotService requirementSnapshotService,
    IAdmissionQuestionSnapshotService questionSnapshotService,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UploadAdmissionAttachmentCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<UploadAdmissionAttachmentCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        UploadAdmissionAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileSize <= 0)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Attachment type or format is invalid.",
                AdmissionErrorCodes.AttachmentTypeInvalid);
        }

        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionApplicationDetailDto>(localizer);
        }

        if (request.RequirementSnapshotId is not null && request.QuestionSnapshotId is not null)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Attachment type or format is invalid.",
                AdmissionErrorCodes.AttachmentTypeInvalid);
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

        if (application.Status == AdmissionApplicationStatus.Draft)
        {
            await requirementSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken);
            await questionSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken: cancellationToken);
        }
        else if (application.Status == AdmissionApplicationStatus.MissingDocuments)
        {
            var active = application.ActiveMissingItemsRequest;
            var requirementOk = request.RequirementSnapshotId is { } reqId &&
                active?.Items.Any(item =>
                    item.Kind == AdmissionMissingItemKind.RequirementSnapshot &&
                    item.RequirementSnapshotId == reqId) == true;
            var questionOk = request.QuestionSnapshotId is { } qId &&
                active?.Items.Any(item =>
                    item.Kind == AdmissionMissingItemKind.QuestionSnapshot &&
                    item.QuestionSnapshotId == qId) == true;
            if (!requirementOk && !questionOk)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Attachment target was not requested for correction.",
                    AdmissionErrorCodes.MissingItemNotRequested);
            }
        }

        AdmissionApplicationRequirementSnapshot? requirementSnapshot = null;
        AdmissionApplicationQuestionSnapshot? questionSnapshot = null;
        AdmissionRequiredDocumentCode? documentCode = null;

        if (request.RequirementSnapshotId is { } requirementSnapshotId)
        {
            var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken);
            requirementSnapshot = loaded?.RequirementSnapshots
                .FirstOrDefault(snapshot => snapshot.Id == requirementSnapshotId);
            if (requirementSnapshot is null)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Requirement snapshot not found.",
                    AdmissionErrorCodes.RequirementSnapshotNotFound);
            }

            if (requirementSnapshot.Kind != AdmissionRequirementKind.ApplicationDocument)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Attachment type or format is invalid.",
                    AdmissionErrorCodes.RequirementDocumentRules);
            }

            documentCode = requirementSnapshot.DocumentCode;
            if (!ValidateFileForRequirementSnapshot(
                    request.OriginalFileName,
                    request.FileSize,
                    requirementSnapshot,
                    out var ruleCode))
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    ruleCode == AdmissionErrorCodes.AttachmentTooLarge
                        ? "Attachment exceeds the maximum allowed size."
                        : "Attachment type or format is invalid.",
                    ruleCode ?? AdmissionErrorCodes.RequirementDocumentRules);
            }
        }
        else if (request.QuestionSnapshotId is { } questionSnapshotId)
        {
            var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken);
            questionSnapshot = loaded?.QuestionSnapshots
                .FirstOrDefault(snapshot => snapshot.Id == questionSnapshotId);
            if (questionSnapshot is null)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Question snapshot not found.",
                    AdmissionErrorCodes.QuestionSnapshotNotFound);
            }

            if (questionSnapshot.QuestionType != AdmissionQuestionType.File)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Attachment type or format is invalid.",
                    AdmissionErrorCodes.QuestionFileRules);
            }

            if (!ValidateFileForQuestionSnapshot(
                    request.OriginalFileName,
                    request.FileSize,
                    questionSnapshot,
                    out var ruleCode))
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    ruleCode == AdmissionErrorCodes.AttachmentTooLarge
                        ? "Attachment exceeds the maximum allowed size."
                        : "Attachment type or format is invalid.",
                    ruleCode ?? AdmissionErrorCodes.QuestionFileRules);
            }
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
                    Category = $"admission-applications/{application.Id:N}",
                },
                cancellationToken);
        }
        catch (PrivateFileValidationException exception)
        {
            logger.LogWarning("Admission attachment upload rejected: {Code}.", exception.ErrorCode);
            return AdmissionResults.MapPrivateFileError<AdmissionApplicationDetailDto>(exception);
        }

        var safeName = AdmissionMapping.SafeOriginalFileName(request.OriginalFileName);
        var attachmentType = documentCode is { } code
            ? AdmissionRequirementCatalog.ToAttachmentType(code)
            : request.AttachmentType;

        AdmissionApplicationAttachment? replaced = null;
        if (requirementSnapshot is not null)
        {
            var detail = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken);
            var existingId = detail?.Attachments
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
        else if (questionSnapshot is not null)
        {
            var detail = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken);
            var existingId = detail?.Attachments
                .FirstOrDefault(attachment => attachment.QuestionSnapshotId == questionSnapshot.Id)
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

        Guid savedAttachmentId;
        if (replaced is not null)
        {
            var previousKey = replaced.StorageKey;
            replaced.ReplaceFile(
                safeName,
                stored.ContentType,
                stored.SizeBytes,
                stored.StoredFileReference,
                userId);
            if (documentCode is { } linkedCode)
            {
                replaced.LinkRequirementSnapshot(requirementSnapshot!.Id, linkedCode);
            }
            else if (questionSnapshot is not null)
            {
                replaced.LinkQuestionSnapshot(questionSnapshot.Id);
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
            savedAttachmentId = replaced.Id;
        }
        else
        {
            var attachment = new AdmissionApplicationAttachment(
                application.Id,
                attachmentType,
                safeName,
                stored.ContentType,
                stored.SizeBytes,
                stored.StoredFileReference,
                userId,
                sourceVaultDocumentId: null,
                requirementSnapshot?.Id,
                documentCode,
                questionSnapshot?.Id);

            admissionRepository.AddAttachment(attachment);
            var conflict = await AdmissionResults.TrySaveAsync<AdmissionApplicationDetailDto>(
                unitOfWork,
                cancellationToken);
            if (conflict is not null)
            {
                await privateFileStorage.DeleteAsync(stored.StoredFileReference, cancellationToken);
                return conflict;
            }

            savedAttachmentId = attachment.Id;
        }

        if (application.Status == AdmissionApplicationStatus.MissingDocuments)
        {
            var item = application.ActiveMissingItemsRequest?.Items.FirstOrDefault(entry =>
                (requirementSnapshot is not null &&
                 entry.Kind == AdmissionMissingItemKind.RequirementSnapshot &&
                 entry.RequirementSnapshotId == requirementSnapshot.Id) ||
                (questionSnapshot is not null &&
                 entry.Kind == AdmissionMissingItemKind.QuestionSnapshot &&
                 entry.QuestionSnapshotId == questionSnapshot.Id));
            item?.MarkCompleted();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (questionSnapshot is not null)
        {
            var existingAnswer = await admissionRepository.GetOwnedAnswerForUpdateAsync(
                userId,
                application.Id,
                questionSnapshot.Id,
                cancellationToken);
            var answer = existingAnswer ?? new AdmissionApplicationAnswer(application.Id, questionSnapshot.Id);
            answer.SetFile(savedAttachmentId);
            if (existingAnswer is null)
            {
                admissionRepository.AddAnswer(answer);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
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
            "Uploaded admission attachment for application {ApplicationId}.",
            application.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(result, identityProtector));
    }

    private static bool ValidateFileForRequirementSnapshot(
        string originalFileName,
        long fileSize,
        AdmissionApplicationRequirementSnapshot snapshot,
        out string? errorCode)
    {
        var allowed = AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions);
        var extension = Path.GetExtension(originalFileName);
        if (allowed.Count > 0 &&
            !allowed.Contains(AdmissionRequirementCatalog.NormalizeExtension(extension)))
        {
            errorCode = AdmissionErrorCodes.RequirementDocumentRules;
            return false;
        }

        if (snapshot.MaxFileSizeBytes is { } max && fileSize > max)
        {
            errorCode = AdmissionErrorCodes.AttachmentTooLarge;
            return false;
        }

        errorCode = null;
        return true;
    }

    private static bool ValidateFileForQuestionSnapshot(
        string originalFileName,
        long fileSize,
        AdmissionApplicationQuestionSnapshot snapshot,
        out string? errorCode)
    {
        var allowed = AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions);
        var extension = Path.GetExtension(originalFileName);
        if (allowed.Count > 0 &&
            !allowed.Contains(AdmissionRequirementCatalog.NormalizeExtension(extension)))
        {
            errorCode = AdmissionErrorCodes.QuestionFileRules;
            return false;
        }

        if (snapshot.MaxFileSizeBytes is { } max && fileSize > max)
        {
            errorCode = AdmissionErrorCodes.AttachmentTooLarge;
            return false;
        }

        errorCode = null;
        return true;
    }
}
