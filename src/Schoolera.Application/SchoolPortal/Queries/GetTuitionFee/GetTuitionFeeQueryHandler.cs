using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetTuitionFee;

public sealed record GetTuitionFeeQuery(Guid SchoolId, Guid FeeId) : IRequest<Result<TuitionFeeDto>>;

public sealed class GetTuitionFeeQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetTuitionFeeQuery, Result<TuitionFeeDto>>
{
    public async Task<Result<TuitionFeeDto>> Handle(
        GetTuitionFeeQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded)
        {
            return Result<TuitionFeeDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        
        var access = accessResult.Data!;
        var permissionCheck = SchoolPortalAccess.RequirePermission<TuitionFeeDto>(
            access, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var fee = await repository.GetTuitionFeeForWriteAsync(
            request.SchoolId,
            request.FeeId,
            cancellationToken);
        if (fee is null)
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.FeeNotFound);
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<TuitionFeeDto>(
            access, fee.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        return Result<TuitionFeeDto>.Success(SchoolPortalReadModel.ToTuitionFee(fee));
    }
}
