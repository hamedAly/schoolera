using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolBranch;

public sealed record GetSchoolBranchQuery(Guid SchoolId, Guid BranchId) : IRequest<Result<SchoolBranchDto>>;

public sealed class GetSchoolBranchQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolBranchQuery, Result<SchoolBranchDto>>
{
    public async Task<Result<SchoolBranchDto>> Handle(
        GetSchoolBranchQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded)
        {
            return Result<SchoolBranchDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        
        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolBranchDto>(
            accessResult.Data!, SchoolPortalPermission.ManageBranches, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

var branch = await repository.GetBranchForWriteAsync(
            request.SchoolId,
            request.BranchId,
            cancellationToken);

        return branch is null
            ? SchoolPortalResults.FailureForCode<SchoolBranchDto>(localizer, SchoolPortalErrorCodes.BranchNotFound)
            : Result<SchoolBranchDto>.Success(SchoolPortalReadModel.ToBranch(branch));
    }
}
