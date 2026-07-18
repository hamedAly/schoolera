using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Application.SchoolOnboarding.Queries.GetDocumentTypes;

public sealed record GetOnboardingDocumentTypesQuery : IRequest<Result<OnboardingDocumentTypesResponseDto>>;

public sealed class GetOnboardingDocumentTypesQueryHandler(
    ISchoolOnboardingRepository repository,
    IPrivateFileStorage privateFileStorage,
    ILogger<GetOnboardingDocumentTypesQueryHandler> logger)
    : IRequestHandler<GetOnboardingDocumentTypesQuery, Result<OnboardingDocumentTypesResponseDto>>
{
    public async Task<Result<OnboardingDocumentTypesResponseDto>> Handle(
        GetOnboardingDocumentTypesQuery request,
        CancellationToken cancellationToken)
    {
        var types = await repository.ListActiveDocumentTypesAsync(cancellationToken);
        var policy = privateFileStorage.Policy;

        logger.LogInformation("Returning {Count} active onboarding document types.", types.Count);

        var response = new OnboardingDocumentTypesResponseDto(
            types.Select(OnboardingDocumentTypeDto.FromEntity).ToArray(),
            policy.MaxFileSizeBytes,
            policy.AllowedExtensions);

        return Result<OnboardingDocumentTypesResponseDto>.Success(response);
    }
}
