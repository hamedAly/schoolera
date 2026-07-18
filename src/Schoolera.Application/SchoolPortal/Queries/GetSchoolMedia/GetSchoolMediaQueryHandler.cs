using MediatR;

using Microsoft.Extensions.Localization;

using Schoolera.Application.Common.Interfaces;

using Schoolera.Application.Common.Models;

using Schoolera.Application.Resources;

using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;

using Schoolera.Application.SchoolPortal.Constants;

using Schoolera.Application.SchoolPortal.Dtos;



namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolMedia;



public sealed record GetSchoolMediaQuery(Guid SchoolId) : IRequest<Result<SchoolMediaDto>>;



public sealed class GetSchoolMediaQueryHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IStringLocalizer<SchoolPortalMessages> localizer)

    : IRequestHandler<GetSchoolMediaQuery, Result<SchoolMediaDto>>

{

    public async Task<Result<SchoolMediaDto>> Handle(

        GetSchoolMediaQuery request,

        CancellationToken cancellationToken)

    {

        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);

        if (!accessResult.Succeeded)

        {

            return Result<SchoolMediaDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);

        }



        
        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolMediaDto>(
            accessResult.Data!, SchoolPortalPermission.ManageGallery, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

var school = await repository.GetSchoolProfileAsync(request.SchoolId, cancellationToken);

        if (school is null)

        {

            return SchoolPortalResults.FailureForCode<SchoolMediaDto>(

                localizer, SchoolPortalErrorCodes.SchoolNotFound);

        }



        var images = await repository.ListImagesAsync(request.SchoolId, cancellationToken);

        var gallery = images

            .Where(image => image.IsActive)

            .Select(SchoolPortalReadModel.ToGalleryImage)

            .ToArray();



        return Result<SchoolMediaDto>.Success(new SchoolMediaDto(school.LogoUrl, school.CoverUrl, gallery));

    }

}


