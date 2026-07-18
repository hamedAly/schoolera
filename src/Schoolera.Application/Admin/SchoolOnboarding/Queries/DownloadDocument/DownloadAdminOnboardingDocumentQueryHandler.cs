using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Application.Admin.SchoolOnboarding.Queries.DownloadDocument;

public sealed record DownloadAdminOnboardingDocumentQuery(Guid ApplicationId, Guid DocumentId)
    : IRequest<Result<OnboardingDocumentDownloadDto>>;

public sealed class DownloadAdminOnboardingDocumentQueryHandler(
    ISchoolOnboardingRepository repository,
    IPrivateFileStorage privateFileStorage,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<DownloadAdminOnboardingDocumentQueryHandler> logger)
    : IRequestHandler<DownloadAdminOnboardingDocumentQuery, Result<OnboardingDocumentDownloadDto>>
{
    public async Task<Result<OnboardingDocumentDownloadDto>> Handle(
        DownloadAdminOnboardingDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var document = await repository.GetDocumentByIdAsync(request.DocumentId, cancellationToken);
        if (document is null || document.ApplicationId != request.ApplicationId)
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
