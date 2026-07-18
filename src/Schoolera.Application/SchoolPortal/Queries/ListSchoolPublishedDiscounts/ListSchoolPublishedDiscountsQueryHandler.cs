using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolPublishedDiscounts;

public sealed record ListSchoolPublishedDiscountsQuery(Guid SchoolId)
    : IRequest<Result<IReadOnlyList<SchoolPublishedDiscountDto>>>;

public sealed class ListSchoolPublishedDiscountsQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolPublishedDiscountsQuery, Result<IReadOnlyList<SchoolPublishedDiscountDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolPublishedDiscountDto>>> Handle(
        ListSchoolPublishedDiscountsQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolPublishedDiscountDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolPublishedDiscountDto>>(
            accessResult.Data, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var now = DateTimeOffset.UtcNow;
        var items = await repository.ListPublishedDiscountsAsync(request.SchoolId, cancellationToken);
        return Result<IReadOnlyList<SchoolPublishedDiscountDto>>.Success(
            items.Select(item => SchoolPortalReadModel.ToDiscount(item, now)).ToArray());
    }
}
