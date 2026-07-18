using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolBranches;

public sealed record ListSchoolBranchesQuery(Guid SchoolId) : IRequest<Result<IReadOnlyList<SchoolBranchDto>>>;

public sealed class ListSchoolBranchesQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolBranchesQuery, Result<IReadOnlyList<SchoolBranchDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolBranchDto>>> Handle(
        ListSchoolBranchesQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolBranchDto>>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolBranchDto>>(
            accessResult.Data, SchoolPortalPermission.ManageBranches, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var branches = await repository.ListBranchesAsync(request.SchoolId, cancellationToken);
        return Result<IReadOnlyList<SchoolBranchDto>>.Success(
            branches.Select(SchoolPortalReadModel.ToBranch).ToArray());
    }
}
