using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListTuitionFees;

public sealed record ListTuitionFeesQuery(Guid SchoolId, Guid? BranchId = null)
    : IRequest<Result<IReadOnlyList<TuitionFeeDto>>>;

public sealed class ListTuitionFeesQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListTuitionFeesQuery, Result<IReadOnlyList<TuitionFeeDto>>>
{
    public async Task<Result<IReadOnlyList<TuitionFeeDto>>> Handle(
        ListTuitionFeesQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<TuitionFeeDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<TuitionFeeDto>>(
            access, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        if (request.BranchId is { } branchId)
        {
            var branchCheck = SchoolPortalAccess.RequireBranch<IReadOnlyList<TuitionFeeDto>>(
                access, branchId, localizer);
            if (!branchCheck.Succeeded)
            {
                return branchCheck;
            }
        }

        var fees = await repository.ListTuitionFeesAsync(
            request.SchoolId,
            request.BranchId,
            cancellationToken);

        if (!access.AllowsAllBranches)
        {
            fees = fees
                .Where(fee => access.CanAccessBranch(fee.SchoolBranchId))
                .ToArray();
        }

        return Result<IReadOnlyList<TuitionFeeDto>>.Success(
            fees.Select(SchoolPortalReadModel.ToTuitionFee).ToArray());
    }
}
