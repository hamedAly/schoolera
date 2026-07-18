using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Application.SchoolOnboarding.Queries.DownloadMyDocument;

public sealed record DownloadOwnerOnboardingDocumentQuery(Guid DocumentId)
    : IRequest<Result<OnboardingDocumentDownloadDto>>;

public sealed class DownloadOwnerOnboardingDocumentQueryHandler(
    ISchoolOnboardingRepository repository,
    IPrivateFileStorage privateFileStorage,
    ICurrentUser currentUser,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<DownloadOwnerOnboardingDocumentQueryHandler> logger)
    : IRequestHandler<DownloadOwnerOnboardingDocumentQuery, Result<OnboardingDocumentDownloadDto>>
{
    public async Task<Result<OnboardingDocumentDownloadDto>> Handle(
        DownloadOwnerOnboardingDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = OnboardingOwnerAccess.ResolveOwnerId(currentUser);
        if (ownerId is null)
        {
            return OnboardingResults.Failure<OnboardingDocumentDownloadDto>(
                localizer, "OwnerRoleRequired", OnboardingErrorCodes.OwnerRoleRequired);
        }

        var document = await repository.GetDocumentByIdAsync(request.DocumentId, cancellationToken);

        // Non-enumerating 404 for missing documents and for documents owned by others.
        if (document is null || document.Application.OwnerUserId != ownerId.Value)
        {
            return OnboardingResults.Failure<OnboardingDocumentDownloadDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        var stream = await privateFileStorage.OpenReadAsync(document.StoredFileReference, cancellationToken);
        if (stream is null)
        {
            logger.LogWarning("Stored file missing for onboarding document {DocumentId}.", document.Id);
            return OnboardingResults.Failure<OnboardingDocumentDownloadDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        return Result<OnboardingDocumentDownloadDto>.Success(new OnboardingDocumentDownloadDto(
            stream,
            document.ContentType,
            OnboardingDownloadName.Sanitize(document.OriginalFileName)));
    }
}
