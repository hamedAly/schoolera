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
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolOnboarding.Commands.SubmitApplication;

public sealed record SubmitOnboardingApplicationCommand : IRequest<Result<MyOnboardingApplicationDto>>;

public sealed class SubmitOnboardingApplicationCommandHandler(
    ISchoolOnboardingRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<SubmitOnboardingApplicationCommandHandler> logger)
    : IRequestHandler<SubmitOnboardingApplicationCommand, Result<MyOnboardingApplicationDto>>
{
    public async Task<Result<MyOnboardingApplicationDto>> Handle(
        SubmitOnboardingApplicationCommand request,
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

        if (application.Status != SchoolOnboardingStatus.Draft)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "InvalidStatusTransition", OnboardingErrorCodes.InvalidStatusTransition);
        }

        var blockingCodes = await OnboardingSubmission.GetBlockingCodesAsync(application, repository, cancellationToken);
        if (blockingCodes.Count > 0)
        {
            return OnboardingResults.FailureForCodes<MyOnboardingApplicationDto>(localizer, blockingCodes);
        }

        var now = DateTimeOffset.UtcNow;
        application.Submit(now);
        application.AddStatusHistory(new SchoolOnboardingStatusHistory(
            application.Id, SchoolOnboardingStatus.Draft, SchoolOnboardingStatus.Submitted, ownerId.Value, null, null));

        var conflict = await OnboardingResults.TrySaveAsync<MyOnboardingApplicationDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Submitted onboarding application {ApplicationId}.", application.Id);

        return Result<MyOnboardingApplicationDto>.Success(
            await OnboardingReadModel.BuildAsync(application, repository, cancellationToken));
    }
}
