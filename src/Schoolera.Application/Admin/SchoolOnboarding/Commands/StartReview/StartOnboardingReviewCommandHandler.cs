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

namespace Schoolera.Application.Admin.SchoolOnboarding.Commands.StartReview;

public sealed record StartOnboardingReviewCommand(Guid ApplicationId)
    : IRequest<Result<AdminOnboardingDetailDto>>;

public sealed class StartOnboardingReviewCommandHandler(
    ISchoolOnboardingRepository repository,
    IUserDirectory userDirectory,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<StartOnboardingReviewCommandHandler> logger)
    : IRequestHandler<StartOnboardingReviewCommand, Result<AdminOnboardingDetailDto>>
{
    public async Task<Result<AdminOnboardingDetailDto>> Handle(
        StartOnboardingReviewCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId;
        if (actorId is null)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "Forbidden", OnboardingErrorCodes.Forbidden);
        }

        var application = await repository.GetByIdAsync(request.ApplicationId, includeChildren: true, cancellationToken);
        if (application is null)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        if (application.Status != SchoolOnboardingStatus.Submitted)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "InvalidStatusTransition", OnboardingErrorCodes.InvalidStatusTransition);
        }

        application.StartReview(actorId.Value, DateTimeOffset.UtcNow);
        application.AddStatusHistory(new SchoolOnboardingStatusHistory(
            application.Id,
            SchoolOnboardingStatus.Submitted,
            SchoolOnboardingStatus.UnderReview,
            actorId.Value,
            null,
            null));

        var conflict = await OnboardingResults.TrySaveAsync<AdminOnboardingDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Started review of onboarding application {ApplicationId}.", application.Id);

        return Result<AdminOnboardingDetailDto>.Success(
            await OnboardingAdminReadModel.BuildAsync(application, repository, userDirectory, cancellationToken));
    }
}
