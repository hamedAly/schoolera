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

namespace Schoolera.Application.SchoolOnboarding.Commands.SaveAuthorizedRepresentative;

public sealed record SaveAuthorizedRepresentativeCommand(
    string? FullNameAr,
    string? FullNameEn,
    string? NationalOrIdentityReference,
    string? JobTitleAr,
    string? JobTitleEn,
    string? Email,
    string? Phone) : IRequest<Result<MyOnboardingApplicationDto>>;

public sealed class SaveAuthorizedRepresentativeCommandHandler(
    ISchoolOnboardingRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<SaveAuthorizedRepresentativeCommandHandler> logger)
    : IRequestHandler<SaveAuthorizedRepresentativeCommand, Result<MyOnboardingApplicationDto>>
{
    public async Task<Result<MyOnboardingApplicationDto>> Handle(
        SaveAuthorizedRepresentativeCommand request,
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

        application.SaveAuthorizedRepresentative(
            request.FullNameAr,
            request.FullNameEn,
            request.NationalOrIdentityReference,
            request.JobTitleAr,
            request.JobTitleEn,
            request.Email,
            request.Phone);
        application.MarkStepReached(SchoolOnboardingStep.SchoolDetails);

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

        logger.LogInformation("Saved authorized-representative step for application {ApplicationId}.", application.Id);

        return Result<MyOnboardingApplicationDto>.Success(
            await OnboardingReadModel.BuildAsync(application, repository, cancellationToken));
    }
}
