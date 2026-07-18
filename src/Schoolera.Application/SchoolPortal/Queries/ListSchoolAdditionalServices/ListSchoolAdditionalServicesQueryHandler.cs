using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolAdditionalServices;

public sealed record ListSchoolAdditionalServicesQuery(Guid SchoolId)
    : IRequest<Result<IReadOnlyList<SchoolAdditionalServiceDto>>>;

public sealed class ListSchoolAdditionalServicesQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolAdditionalServicesQuery, Result<IReadOnlyList<SchoolAdditionalServiceDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolAdditionalServiceDto>>> Handle(
        ListSchoolAdditionalServicesQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolAdditionalServiceDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolAdditionalServiceDto>>(
            accessResult.Data, SchoolPortalPermission.ManageServices, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var services = await repository.ListServicesAsync(request.SchoolId, cancellationToken);
        return Result<IReadOnlyList<SchoolAdditionalServiceDto>>.Success(
            services.Select(SchoolPortalReadModel.ToService).ToArray());
    }
}
