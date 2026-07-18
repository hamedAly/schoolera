using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolStageOfferings;

public sealed record ListSchoolStageOfferingsQuery(Guid SchoolId, Guid? BranchId = null)
    : IRequest<Result<IReadOnlyList<SchoolStageOfferingDto>>>;

public sealed class ListSchoolStageOfferingsQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolStageOfferingsQuery, Result<IReadOnlyList<SchoolStageOfferingDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolStageOfferingDto>>> Handle(
        ListSchoolStageOfferingsQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolStageOfferingDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolStageOfferingDto>>(
            access, SchoolPortalPermission.ManageOfferings, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        if (request.BranchId is { } branchId)
        {
            var branchCheck = SchoolPortalAccess.RequireBranch<IReadOnlyList<SchoolStageOfferingDto>>(
                access, branchId, localizer);
            if (!branchCheck.Succeeded)
            {
                return branchCheck;
            }
        }

        var offerings = await repository.ListOfferingsAsync(
            request.SchoolId,
            request.BranchId,
            cancellationToken);

        if (!access.AllowsAllBranches)
        {
            offerings = offerings
                .Where(offering => access.CanAccessBranch(offering.SchoolBranchId))
                .ToArray();
        }

        return Result<IReadOnlyList<SchoolStageOfferingDto>>.Success(
            offerings.Select(SchoolPortalReadModel.ToOffering).ToArray());
    }
}
