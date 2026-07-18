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

namespace Schoolera.Application.SchoolOnboarding.Commands.SaveOrganization;

public sealed record SaveOrganizationCommand(
    string? OrganizationNameAr,
    string? OrganizationNameEn,
    string? LegalName,
    string? CountryCode,
    string? RegistrationOrLicenseNumber,
    string? TaxRegistrationNumber,
    string? LegalForm,
    string? OrganizationAddress,
    string? OrganizationWebsite) : IRequest<Result<MyOnboardingApplicationDto>>;

public sealed class SaveOrganizationCommandHandler(
    ISchoolOnboardingRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<SaveOrganizationCommandHandler> logger)
    : IRequestHandler<SaveOrganizationCommand, Result<MyOnboardingApplicationDto>>
{
    public async Task<Result<MyOnboardingApplicationDto>> Handle(
        SaveOrganizationCommand request,
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

        var normalized = string.IsNullOrWhiteSpace(request.RegistrationOrLicenseNumber)
            ? null
            : request.RegistrationOrLicenseNumber.Trim().ToUpperInvariant();
        var country = request.CountryCode?.Trim().ToUpperInvariant();

        if (normalized is not null && !string.IsNullOrWhiteSpace(country) &&
            await repository.RegistrationNumberExistsAsync(normalized, country, application.Id, cancellationToken))
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "DuplicateRegistrationNumber", OnboardingErrorCodes.DuplicateRegistrationNumber);
        }

        application.SaveOrganization(
            request.OrganizationNameAr,
            request.OrganizationNameEn,
            request.LegalName,
            request.CountryCode,
            request.RegistrationOrLicenseNumber,
            request.TaxRegistrationNumber,
            request.LegalForm,
            request.OrganizationAddress,
            request.OrganizationWebsite);
        application.MarkStepReached(SchoolOnboardingStep.AuthorizedRepresentative);

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

        logger.LogInformation("Saved organization step for onboarding application {ApplicationId}.", application.Id);

        return Result<MyOnboardingApplicationDto>.Success(
            await OnboardingReadModel.BuildAsync(application, repository, cancellationToken));
    }
}
