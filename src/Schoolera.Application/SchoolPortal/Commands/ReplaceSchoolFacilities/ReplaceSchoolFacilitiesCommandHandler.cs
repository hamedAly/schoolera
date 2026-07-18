using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.ReplaceSchoolFacilities;

public sealed record ReplaceSchoolFacilitiesCommand(
    Guid SchoolId,
    ReplaceSchoolFacilitiesRequest Body) : IRequest<Result<IReadOnlyList<SchoolFacilityListItemDto>>>;

public sealed class ReplaceSchoolFacilitiesCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ReplaceSchoolFacilitiesCommandHandler> logger)
    : IRequestHandler<ReplaceSchoolFacilitiesCommand, Result<IReadOnlyList<SchoolFacilityListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolFacilityListItemDto>>> Handle(
        ReplaceSchoolFacilitiesCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolFacilityListItemDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<IReadOnlyList<SchoolFacilityListItemDto>>(
            accessResult.Data, SchoolPortalPermission.ManageFacilities, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var facilityIds = request.Body.FacilityIds.Distinct().ToArray();
        if (facilityIds.Length > 0)
        {
            var validFacilities = await repository.GetActiveFacilitiesByIdsAsync(facilityIds, cancellationToken);
            if (validFacilities.Count != facilityIds.Length)
            {
                return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolFacilityListItemDto>>(
                    localizer, SchoolPortalErrorCodes.FacilityNotFound);
            }
        }

        await repository.ReplaceSchoolFacilitiesAsync(request.SchoolId, facilityIds, cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<IReadOnlyList<SchoolFacilityListItemDto>>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var selectedIds = facilityIds.ToHashSet();
        var catalogFacilities = await taxonomyRepository.ListActiveFacilitiesAsync(cancellationToken);
        var items = catalogFacilities
            .Select(facility => new SchoolFacilityListItemDto(
                facility.Id,
                facility.NameAr,
                facility.NameEn,
                facility.Slug,
                facility.IconKey,
                selectedIds.Contains(facility.Id)))
            .ToArray();

        return Result<IReadOnlyList<SchoolFacilityListItemDto>>.Success(items);
    }
}
