using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Enums;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Application.Payments.Queries.ListSchoolPayableItems;

public sealed record ListSchoolPayableItemsQuery(Guid SchoolId)
    : IRequest<Result<IReadOnlyList<SchoolPayableItemAdminDto>>>;

public sealed class ListSchoolPayableItemsQueryHandler(
    ISchoolPortalAccess portalAccess,
    IPaymentRepository paymentRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolPayableItemsQuery, Result<IReadOnlyList<SchoolPayableItemAdminDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolPayableItemAdminDto>>> Handle(
        ListSchoolPayableItemsQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolPayableItemAdminDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolPayableItemAdminDto>>(
            accessResult.Data, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var items = await paymentRepository.ListPayableItemsForSchoolAsync(
            request.SchoolId, cancellationToken);
        return Result<IReadOnlyList<SchoolPayableItemAdminDto>>.Success(
            items.Select(PaymentMapping.ToSchoolPayableAdminDto).ToList());
    }
}
