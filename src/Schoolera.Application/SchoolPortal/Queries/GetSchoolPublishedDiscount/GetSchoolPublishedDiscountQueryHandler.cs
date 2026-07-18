using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolPublishedDiscount;

public sealed record GetSchoolPublishedDiscountQuery(Guid SchoolId, Guid DiscountId)
    : IRequest<Result<SchoolPublishedDiscountDto>>;

public sealed class GetSchoolPublishedDiscountQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolPublishedDiscountQuery, Result<SchoolPublishedDiscountDto>>
{
    public async Task<Result<SchoolPublishedDiscountDto>> Handle(
        GetSchoolPublishedDiscountQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolPublishedDiscountDto>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolPublishedDiscountDto>(
            accessResult.Data, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var item = await repository.GetPublishedDiscountForWriteAsync(
            request.SchoolId, request.DiscountId, cancellationToken);
        if (item is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                localizer, SchoolPortalErrorCodes.DiscountNotFound);
        }

        return Result<SchoolPublishedDiscountDto>.Success(
            SchoolPortalReadModel.ToDiscount(item, DateTimeOffset.UtcNow));
    }
}
