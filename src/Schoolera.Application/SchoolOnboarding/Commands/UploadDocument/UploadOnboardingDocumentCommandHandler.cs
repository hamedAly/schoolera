using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Commands.UploadDocument;

public sealed record UploadOnboardingDocumentCommand(
    Guid DocumentTypeId,
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long FileSize) : IRequest<Result<MyOnboardingApplicationDto>>;

public sealed class UploadOnboardingDocumentCommandHandler(
    ISchoolOnboardingRepository repository,
    IPrivateFileStorage privateFileStorage,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<UploadOnboardingDocumentCommandHandler> logger)
    : IRequestHandler<UploadOnboardingDocumentCommand, Result<MyOnboardingApplicationDto>>
{
    public async Task<Result<MyOnboardingApplicationDto>> Handle(
        UploadOnboardingDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = OnboardingOwnerAccess.ResolveOwnerId(currentUser);
        if (ownerId is null)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "OwnerRoleRequired", OnboardingErrorCodes.OwnerRoleRequired);
        }

        var application = await repository.GetByOwnerAsync(ownerId.Value, includeChildren: true, cancellationToken);
        if (application is null)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        if (!application.IsEditable)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "NotEditable", OnboardingErrorCodes.NotEditable);
        }

        var documentType = await repository.GetDocumentTypeByIdAsync(request.DocumentTypeId, cancellationToken);
        if (documentType is null || !documentType.IsActive)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "InvalidDocumentType", OnboardingErrorCodes.InvalidDocumentType);
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
                    Category = application.Id.ToString("N"),
                },
                cancellationToken);
        }
        catch (PrivateFileValidationException exception)
        {
            logger.LogWarning("Onboarding upload rejected: {Code}.", exception.ErrorCode);
            return OnboardingResults.FailureForCode<MyOnboardingApplicationDto>(localizer, exception.ErrorCode);
        }

        var now = DateTimeOffset.UtcNow;
        var superseded = application.Documents
            .Where(document => document.IsCurrent && document.DocumentTypeId == documentType.Id)
            .ToList();

        foreach (var document in superseded)
        {
            document.Retire(now);
        }

        if (superseded.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var newDocument = new SchoolOnboardingDocument(
            application.Id,
            documentType.Id,
            SafeOriginalName(request.OriginalFileName),
            stored.StoredFileReference,
            stored.ContentType,
            stored.SizeBytes,
            stored.Sha256Hash,
            ownerId.Value);
        application.AddDocument(newDocument);

        var conflict = await OnboardingResults.TrySaveAsync<MyOnboardingApplicationDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            await privateFileStorage.DeleteAsync(stored.StoredFileReference, cancellationToken);
            return conflict;
        }

        foreach (var document in superseded)
        {
            await privateFileStorage.DeleteAsync(document.StoredFileReference, cancellationToken);
        }

        logger.LogInformation(
            "Uploaded onboarding document {DocumentId} (type {Code}) for application {ApplicationId}.",
            newDocument.Id,
            documentType.Code,
            application.Id);

        return Result<MyOnboardingApplicationDto>.Success(
            await OnboardingReadModel.BuildAsync(application, repository, cancellationToken));
    }

    private static string SafeOriginalName(string originalFileName)
    {
        var name = Path.GetFileName(originalFileName ?? string.Empty);
        return string.IsNullOrWhiteSpace(name) ? "document" : name;
    }
}
