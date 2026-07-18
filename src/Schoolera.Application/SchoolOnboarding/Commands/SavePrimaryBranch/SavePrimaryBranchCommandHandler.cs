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

namespace Schoolera.Application.SchoolOnboarding.Commands.SavePrimaryBranch;

public sealed record SavePrimaryBranchCommand(
    Guid? CityId,
    Guid? DistrictId,
    string? AddressLineAr,
    string? AddressLineEn,
    string? BuildingNumber,
    string? StreetName,
    string? Landmark,
    string? PostalCode,
    string? LocalAddressReference,
    decimal? Latitude,
    decimal? Longitude,
    string? PublicPhone,
    string? PublicEmail,
    string? WhatsAppOrAlternatePhone) : IRequest<Result<MyOnboardingApplicationDto>>;

public sealed class SavePrimaryBranchCommandHandler(
    ISchoolOnboardingRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<SavePrimaryBranchCommandHandler> logger)
    : IRequestHandler<SavePrimaryBranchCommand, Result<MyOnboardingApplicationDto>>
{
    public async Task<Result<MyOnboardingApplicationDto>> Handle(
        SavePrimaryBranchCommand request,
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

        if (request.CityId is { } cityId && request.DistrictId is { } districtId &&
            !await repository.DistrictBelongsToCityAsync(districtId, cityId, cancellationToken))
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "CityDistrictMismatch", OnboardingErrorCodes.CityDistrictMismatch);
        }

        application.SavePrimaryBranch(
            request.CityId,
            request.DistrictId,
            request.AddressLineAr,
            request.AddressLineEn,
            request.BuildingNumber,
            request.StreetName,
            request.Landmark,
            request.PostalCode,
            request.LocalAddressReference,
            request.Latitude,
            request.Longitude,
            request.PublicPhone,
            request.PublicEmail,
            request.WhatsAppOrAlternatePhone);
        application.MarkStepReached(SchoolOnboardingStep.Documents);

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

        logger.LogInformation("Saved primary-branch step for application {ApplicationId}.", application.Id);

        return Result<MyOnboardingApplicationDto>.Success(
            await OnboardingReadModel.BuildAsync(application, repository, cancellationToken));
    }
}
