using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Application.SchoolOnboarding.Queries.GetMyApplication;

public sealed record GetMyOnboardingApplicationQuery : IRequest<Result<MyOnboardingApplicationDto?>>;

public sealed class GetMyOnboardingApplicationQueryHandler(
    ISchoolOnboardingRepository repository,
    ICurrentUser currentUser,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<GetMyOnboardingApplicationQueryHandler> logger)
    : IRequestHandler<GetMyOnboardingApplicationQuery, Result<MyOnboardingApplicationDto?>>
{
    public async Task<Result<MyOnboardingApplicationDto?>> Handle(
        GetMyOnboardingApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = OnboardingOwnerAccess.ResolveOwnerId(currentUser);
        if (ownerId is null)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto?>(
                localizer, "OwnerRoleRequired", OnboardingErrorCodes.OwnerRoleRequired);
        }

        var application = await repository.GetByOwnerAsync(ownerId.Value, includeChildren: true, cancellationToken);
        if (application is null)
        {
            logger.LogInformation("No onboarding application found for owner {OwnerId}.", ownerId);
            return Result<MyOnboardingApplicationDto?>.Success(null);
        }

        var dto = await OnboardingReadModel.BuildAsync(application, repository, cancellationToken);
        return Result<MyOnboardingApplicationDto?>.Success(dto);
    }
}
