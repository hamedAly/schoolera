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

namespace Schoolera.Application.SchoolOnboarding.Commands.SaveSchoolDetails;

public sealed record SaveSchoolDetailsCommand(
    string? SchoolNameAr,
    string? SchoolNameEn,
    SchoolType? SchoolType,
    GenderType? GenderType,
    int? FoundedYear,
    string? ShortDescriptionAr,
    string? ShortDescriptionEn,
    string? WebsiteUrl,
    string? RequestedSlug) : IRequest<Result<MyOnboardingApplicationDto>>;

public sealed class SaveSchoolDetailsCommandHandler(
    ISchoolOnboardingRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<SaveSchoolDetailsCommandHandler> logger)
    : IRequestHandler<SaveSchoolDetailsCommand, Result<MyOnboardingApplicationDto>>
{
    public async Task<Result<MyOnboardingApplicationDto>> Handle(
        SaveSchoolDetailsCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = OnboardingOwnerAccess.ResolveOwnerId(currentUser);
        if (ownerId is null)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "OwnerRoleRequired", OnboardingErrorCodes.OwnerRoleRequired);
        }

        var application = await repository.GetByOwnerAsync(ownerId.Value, includeChildren: true, cancellationToken);
        var isNew = application is null;
        application ??= new SchoolOnboardingApplication(ownerId.Value);

        if (!application.IsEditable)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "NotEditable", OnboardingErrorCodes.NotEditable);
        }

        application.SaveSchoolDetails(
            request.SchoolNameAr,
            request.SchoolNameEn,
            request.SchoolType,
            request.GenderType,
            request.FoundedYear,
            request.ShortDescriptionAr,
            request.ShortDescriptionEn,
            request.WebsiteUrl,
            request.RequestedSlug);
        application.MarkStepReached(SchoolOnboardingStep.PrimaryBranch);

        if (isNew)
        {
            await repository.AddAsync(application, cancellationToken);
        }

        var conflict = await OnboardingResults.TrySaveAsync<MyOnboardingApplicationDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Saved school-details step for application {ApplicationId}.", application.Id);

        return Result<MyOnboardingApplicationDto>.Success(
            await OnboardingReadModel.BuildAsync(application, repository, cancellationToken));
    }
}
