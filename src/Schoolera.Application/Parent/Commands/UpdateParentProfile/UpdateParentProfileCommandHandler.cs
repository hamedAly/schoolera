using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Parent.Queries.GetParentProfile;
using Schoolera.Application.Resources;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.UpdateParentProfile;

public sealed record UpdateParentProfileCommand(UpdateParentProfileRequest Body)
    : IRequest<Result<ParentProfileDto>>;

public sealed class UpdateParentProfileCommandHandler(
    ICurrentUser currentUser,
    IParentAccountService parentAccountService,
    IParentProfileRepository parentProfileRepository,
    ITaxonomyRepository taxonomyRepository,
    IChildIdentityProtector identityProtector,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UpdateParentProfileCommandHandler> logger)
    : IRequestHandler<UpdateParentProfileCommand, Result<ParentProfileDto>>
{
    public async Task<Result<ParentProfileDto>> Handle(
        UpdateParentProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentProfileDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var body = request.Body;
        var hierarchy = await ValidateLocationHierarchyAsync(body, taxonomyRepository, cancellationToken);
        if (hierarchy is not null)
        {
            return hierarchy;
        }

        await parentAccountService.UpdateBasicAsync(
            userId,
            body.FirstName,
            body.LastName,
            body.Phone,
            body.PreferredLanguage,
            cancellationToken);

        var profile = await parentProfileRepository.GetByUserIdForUpdateAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = new ParentProfile(userId);
            await parentProfileRepository.AddAsync(profile, cancellationToken);
        }

        var father = body.Father ?? new UpdateParentGuardianRequest(null, null, null, null, null, null, null);
        var mother = body.Mother ?? new UpdateParentGuardianRequest(null, null, null, null, null, null, null);

        var fatherIdentity = TryResolveGuardianIdentity(
            father,
            profile.FatherIdentityLastFour,
            identityProtector);
        if (fatherIdentity.Error is not null)
        {
            return fatherIdentity.Error;
        }

        var motherIdentity = TryResolveGuardianIdentity(
            mother,
            profile.MotherIdentityLastFour,
            identityProtector);
        if (motherIdentity.Error is not null)
        {
            return motherIdentity.Error;
        }

        profile.Update(
            body.AlternatePhone,
            body.AddressLine,
            body.Qualification,
            body.Occupation,
            body.CountryId,
            body.GovernorateId,
            body.CityId,
            body.DistrictId,
            body.PreferredContactMethod,
            new GuardianProfileUpdate(
                father.FullName,
                father.Phone,
                father.Email,
                father.Occupation,
                father.Qualification),
            new GuardianProfileUpdate(
                mother.FullName,
                mother.Phone,
                mother.Email,
                mother.Occupation,
                mother.Qualification));

        if (fatherIdentity.Replacement is { } fatherReplacement)
        {
            profile.ReplaceFatherIdentity(
                fatherReplacement.IdentityType,
                fatherReplacement.ProtectedValue,
                fatherReplacement.LookupHash,
                fatherReplacement.LastFour);
        }

        if (motherIdentity.Replacement is { } motherReplacement)
        {
            profile.ReplaceMotherIdentity(
                motherReplacement.IdentityType,
                motherReplacement.ProtectedValue,
                motherReplacement.LookupHash,
                motherReplacement.LastFour);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await parentAccountService.GetAsync(userId, cancellationToken);
        profile = await parentProfileRepository.GetByUserIdAsync(userId, cancellationToken) ?? profile;
        logger.LogInformation("Updated parent profile for user {UserId}.", userId);

        return Result<ParentProfileDto>.Success(
            GetParentProfileQueryHandler.Map(account!, profile, identityProtector));
    }

    private static async Task<Result<ParentProfileDto>?> ValidateLocationHierarchyAsync(
        UpdateParentProfileRequest body,
        ITaxonomyRepository taxonomyRepository,
        CancellationToken cancellationToken)
    {
        // Preserve city/district-only compatibility; when country/governorate are supplied,
        // validate the full Country → Governorate → City → District chain.
        if (body.DistrictId is { } districtId)
        {
            if (body.CityId is not { } cityId)
            {
                return Result<ParentProfileDto>.Failure(
                    ["City is required when a district is selected."],
                    [ParentErrorCodes.InvalidCityDistrict]);
            }

            var district = await taxonomyRepository.GetDistrictByIdAsync(districtId, cancellationToken);
            if (district is null || !district.IsActive || district.CityId != cityId)
            {
                return Result<ParentProfileDto>.Failure(
                    ["City and district combination is invalid."],
                    [ParentErrorCodes.InvalidCityDistrict]);
            }
        }
        else if (body.CityId is { } cityIdOnly)
        {
            var city = await taxonomyRepository.GetCityByIdAsync(cityIdOnly, cancellationToken);
            if (city is null || !city.IsActive)
            {
                return Result<ParentProfileDto>.Failure(
                    ["City and district combination is invalid."],
                    [ParentErrorCodes.InvalidCityDistrict]);
            }
        }

        if (body.CountryId is null && body.GovernorateId is null)
        {
            return null;
        }

        if (body.CountryId is { } countryId)
        {
            var country = await taxonomyRepository.GetCountryByIdAsync(countryId, cancellationToken);
            if (country is null || !country.IsActive)
            {
                return Result<ParentProfileDto>.Failure(
                    ["Location hierarchy is invalid."],
                    [TaxonomyErrorCodes.HierarchyMismatch]);
            }
        }

        if (body.GovernorateId is { } governorateId)
        {
            var governorate = await taxonomyRepository.GetGovernorateByIdAsync(governorateId, cancellationToken);
            if (governorate is null || !governorate.IsActive)
            {
                return Result<ParentProfileDto>.Failure(
                    ["Location hierarchy is invalid."],
                    [TaxonomyErrorCodes.HierarchyMismatch]);
            }

            if (body.CountryId is { } expectedCountryId && governorate.CountryId != expectedCountryId)
            {
                return Result<ParentProfileDto>.Failure(
                    ["Location hierarchy is invalid."],
                    [TaxonomyErrorCodes.HierarchyMismatch]);
            }
        }

        if (body.CityId is { } linkedCityId &&
            (body.CountryId is not null || body.GovernorateId is not null))
        {
            var city = await taxonomyRepository.GetCityByIdAsync(linkedCityId, cancellationToken);
            if (city is null || !city.IsActive)
            {
                return Result<ParentProfileDto>.Failure(
                    ["Location hierarchy is invalid."],
                    [TaxonomyErrorCodes.HierarchyMismatch]);
            }

            if (body.GovernorateId is { } expectedGovernorateId &&
                city.GovernorateId is { } cityGovernorateId &&
                cityGovernorateId != expectedGovernorateId)
            {
                return Result<ParentProfileDto>.Failure(
                    ["Location hierarchy is invalid."],
                    [TaxonomyErrorCodes.HierarchyMismatch]);
            }

            if (body.CountryId is { } expectedCountry &&
                city.GovernorateId is { } cityGovId)
            {
                var cityGovernorate = await taxonomyRepository.GetGovernorateByIdAsync(
                    cityGovId,
                    cancellationToken);
                if (cityGovernorate is null || cityGovernorate.CountryId != expectedCountry)
                {
                    return Result<ParentProfileDto>.Failure(
                        ["Location hierarchy is invalid."],
                        [TaxonomyErrorCodes.HierarchyMismatch]);
                }
            }
        }

        return null;
    }

    private static GuardianIdentityResolution TryResolveGuardianIdentity(
        UpdateParentGuardianRequest guardian,
        string? existingLastFour,
        IChildIdentityProtector identityProtector)
    {
        if (ChildIdentityMasking.IsBlankOrMasked(guardian.IdentityValue, existingLastFour))
        {
            return GuardianIdentityResolution.Preserve();
        }

        if (guardian.IdentityType is null)
        {
            return GuardianIdentityResolution.Fail(
                Result<ParentProfileDto>.Failure(
                    ["Identity type is required when replacing guardian identity."],
                    [ParentErrorCodes.InvalidIdentity]));
        }

        var normalized = identityProtector.Normalize(guardian.IdentityValue!);
        if (!identityProtector.IsValidFormat(guardian.IdentityType.Value, normalized))
        {
            return GuardianIdentityResolution.Fail(
                Result<ParentProfileDto>.Failure(
                    ["Guardian identity value format is invalid."],
                    [ParentErrorCodes.InvalidIdentity]));
        }

        return GuardianIdentityResolution.Replace(new GuardianIdentityReplacement(
            guardian.IdentityType.Value,
            identityProtector.Protect(normalized),
            identityProtector.ComputeLookupHash(normalized),
            identityProtector.ExtractLastFour(normalized)));
    }

    private sealed record GuardianIdentityReplacement(
        ChildIdentityType IdentityType,
        string ProtectedValue,
        string LookupHash,
        string LastFour);

    private sealed record GuardianIdentityResolution(
        GuardianIdentityReplacement? Replacement,
        Result<ParentProfileDto>? Error)
    {
        public static GuardianIdentityResolution Preserve() => new(null, null);

        public static GuardianIdentityResolution Replace(GuardianIdentityReplacement replacement) =>
            new(replacement, null);

        public static GuardianIdentityResolution Fail(Result<ParentProfileDto> error) =>
            new(null, error);
    }
}
