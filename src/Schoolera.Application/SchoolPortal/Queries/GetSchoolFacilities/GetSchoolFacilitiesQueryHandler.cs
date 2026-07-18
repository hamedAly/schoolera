using MediatR;

using Microsoft.Extensions.Localization;

using Schoolera.Application.Common.Interfaces;

using Schoolera.Application.Common.Models;

using Schoolera.Application.Resources;

using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;

using Schoolera.Application.SchoolPortal.Constants;

using Schoolera.Application.SchoolPortal.Dtos;



namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolFacilities;



public sealed record GetSchoolFacilitiesQuery(Guid SchoolId)

    : IRequest<Result<IReadOnlyList<SchoolFacilityListItemDto>>>;



public sealed class GetSchoolFacilitiesQueryHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    ITaxonomyRepository taxonomyRepository,

    IStringLocalizer<SchoolPortalMessages> localizer)

    : IRequestHandler<GetSchoolFacilitiesQuery, Result<IReadOnlyList<SchoolFacilityListItemDto>>>

{

    public async Task<Result<IReadOnlyList<SchoolFacilityListItemDto>>> Handle(

        GetSchoolFacilitiesQuery request,

        CancellationToken cancellationToken)

    {

        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);

        if (!accessResult.Succeeded)

        {

            return Result<IReadOnlyList<SchoolFacilityListItemDto>>.Failure(

                accessResult.Errors, accessResult.ErrorCodes);

        }



        
        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolFacilityListItemDto>>(
            accessResult.Data!, SchoolPortalPermission.ManageFacilities, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

var selectedFacilities = await repository.ListSchoolFacilitiesAsync(request.SchoolId, cancellationToken);

        var selectedIds = selectedFacilities.Select(facility => facility.FacilityId).ToHashSet();



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


