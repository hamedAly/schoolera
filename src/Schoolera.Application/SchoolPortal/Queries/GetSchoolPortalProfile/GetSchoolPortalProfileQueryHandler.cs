using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolPortalProfile;

public sealed record GetSchoolPortalProfileQuery(Guid SchoolId) : IRequest<Result<SchoolPortalProfileDto>>;

public sealed class GetSchoolPortalProfileQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolPortalProfileQuery, Result<SchoolPortalProfileDto>>
{
    public async Task<Result<SchoolPortalProfileDto>> Handle(
        GetSchoolPortalProfileQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolPortalProfileDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolPortalProfileDto>(
            accessResult.Data, SchoolPortalPermission.ViewProfile, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var school = await repository.GetSchoolProfileAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolPortalProfileDto>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        return Result<SchoolPortalProfileDto>.Success(SchoolPortalReadModel.ToProfile(school));
    }
}
