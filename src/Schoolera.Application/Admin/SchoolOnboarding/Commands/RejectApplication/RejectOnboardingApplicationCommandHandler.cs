using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admin.SchoolOnboarding.Commands.RejectApplication;

public sealed record RejectOnboardingApplicationCommand(
    Guid ApplicationId,
    string OwnerVisibleReason,
    string? InternalNote) : IRequest<Result<AdminOnboardingDetailDto>>;

public sealed class RejectOnboardingApplicationCommandHandler(
    ISchoolOnboardingRepository repository,
    IUserDirectory userDirectory,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IAdminPlatformService adminPlatform,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<RejectOnboardingApplicationCommandHandler> logger)
    : IRequestHandler<RejectOnboardingApplicationCommand, Result<AdminOnboardingDetailDto>>
{
    public async Task<Result<AdminOnboardingDetailDto>> Handle(
        RejectOnboardingApplicationCommand request,
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

        if (application.Status != SchoolOnboardingStatus.UnderReview)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "InvalidStatusTransition", OnboardingErrorCodes.InvalidStatusTransition);
        }

        application.Reject(actorId.Value, DateTimeOffset.UtcNow);
        application.AddStatusHistory(new SchoolOnboardingStatusHistory(
            application.Id,
            SchoolOnboardingStatus.UnderReview,
            SchoolOnboardingStatus.Rejected,
            actorId.Value,
            request.OwnerVisibleReason.Trim(),
            string.IsNullOrWhiteSpace(request.InternalNote) ? null : request.InternalNote.Trim()));

        var conflict = await OnboardingResults.TrySaveAsync<AdminOnboardingDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Rejected onboarding application {ApplicationId}.", application.Id);

        await adminPlatform.WriteAuditAsync(
            actorId.Value,
            AdminAuditActions.OnboardingRejected,
            "OnboardingApplication",
            application.Id.ToString(),
            "Rejected",
            cancellationToken);

        return Result<AdminOnboardingDetailDto>.Success(
            await OnboardingAdminReadModel.BuildAsync(application, repository, userDirectory, cancellationToken));
    }
}
