using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Resources;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Queries.GetParentProfile;

public sealed record GetParentProfileQuery : IRequest<Result<ParentProfileDto>>;

public sealed class GetParentProfileQueryHandler(
    ICurrentUser currentUser,
    IParentAccountService parentAccountService,
    IParentProfileRepository parentProfileRepository,
    IChildIdentityProtector identityProtector,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<GetParentProfileQueryHandler> logger)
    : IRequestHandler<GetParentProfileQuery, Result<ParentProfileDto>>
{
    public async Task<Result<ParentProfileDto>> Handle(
        GetParentProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentProfileDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var account = await parentAccountService.GetAsync(userId, cancellationToken);
        if (account is null)
        {
            return Result<ParentProfileDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.ProfileNotFound]);
        }

        var profile = await parentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        logger.LogInformation("Loading parent profile for user {UserId}.", userId);

        if (profile is null)
        {
            return Result<ParentProfileDto>.Success(
                new ParentProfileDto(
                    Guid.Empty,
                    account.Email,
                    account.FirstName,
                    account.LastName,
                    account.Phone,
                    AlternatePhone: null,
                    AddressLine: null,
                    Qualification: null,
                    Occupation: null,
                    CountryId: null,
                    CountryName: null,
                    GovernorateId: null,
                    GovernorateName: null,
                    CityId: null,
                    CityName: null,
                    DistrictId: null,
                    DistrictName: null,
                    PreferredContactMethod.Phone,
                    account.PreferredLanguage,
                    EmptyGuardian(),
                    EmptyGuardian(),
                    IsComplete: false));
        }

        return Result<ParentProfileDto>.Success(Map(account, profile, identityProtector));
    }

    internal static ParentProfileDto Map(
        ParentAccountSnapshot account,
        ParentProfile profile,
        IChildIdentityProtector identityProtector)
    {
        var complete = profile.CityId is not null && profile.DistrictId is not null;
        return new ParentProfileDto(
            profile.Id,
            account.Email,
            account.FirstName,
            account.LastName,
            account.Phone,
            profile.AlternatePhone,
            profile.AddressLine,
            profile.Qualification,
            profile.Occupation,
            profile.CountryId,
            profile.Country is null
                ? null
                : LocalizationDisplayHelper.Pick(profile.Country.NameAr, profile.Country.NameEn),
            profile.GovernorateId,
            profile.Governorate is null
                ? null
                : LocalizationDisplayHelper.Pick(profile.Governorate.NameAr, profile.Governorate.NameEn),
            profile.CityId,
            profile.City is null
                ? null
                : LocalizationDisplayHelper.Pick(profile.City.NameAr, profile.City.NameEn),
            profile.DistrictId,
            profile.District is null
                ? null
                : LocalizationDisplayHelper.Pick(profile.District.NameAr, profile.District.NameEn),
            profile.PreferredContactMethod,
            account.PreferredLanguage,
            MapGuardian(
                profile.FatherFullName,
                profile.FatherPhone,
                profile.FatherEmail,
                profile.FatherOccupation,
                profile.FatherQualification,
                profile.FatherIdentityType,
                profile.FatherIdentityLastFour,
                identityProtector),
            MapGuardian(
                profile.MotherFullName,
                profile.MotherPhone,
                profile.MotherEmail,
                profile.MotherOccupation,
                profile.MotherQualification,
                profile.MotherIdentityType,
                profile.MotherIdentityLastFour,
                identityProtector),
            complete);
    }

    private static ParentGuardianDto EmptyGuardian() =>
        new(null, null, null, null, null, null, null);

    private static ParentGuardianDto MapGuardian(
        string? fullName,
        string? phone,
        string? email,
        string? occupation,
        string? qualification,
        ChildIdentityType? identityType,
        string? identityLastFour,
        IChildIdentityProtector identityProtector) =>
        new(
            fullName,
            phone,
            email,
            occupation,
            qualification,
            identityType,
            string.IsNullOrWhiteSpace(identityLastFour)
                ? null
                : identityProtector.Mask(identityLastFour));
}
