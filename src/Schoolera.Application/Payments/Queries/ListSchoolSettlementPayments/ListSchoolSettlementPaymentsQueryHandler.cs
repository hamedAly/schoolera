using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Queries.ListSchoolSettlementPayments;

public sealed record ListSchoolSettlementPaymentsQuery(Guid SchoolId)
    : IRequest<Result<IReadOnlyList<SchoolSettlementPaymentDto>>>;

public sealed class ListSchoolSettlementPaymentsQueryHandler(
    ISchoolPortalAccess portalAccess,
    IPaymentRepository paymentRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolSettlementPaymentsQuery, Result<IReadOnlyList<SchoolSettlementPaymentDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolSettlementPaymentDto>>> Handle(
        ListSchoolSettlementPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolSettlementPaymentDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolSettlementPaymentDto>>(
            accessResult.Data, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var intents = await paymentRepository.ListIntentsForSchoolAsync(
            request.SchoolId, cancellationToken);
        var reconciliations = await paymentRepository.ListReconciliationAsync(null, cancellationToken);
        var holdIds = reconciliations
            .Where(record =>
                record.PaymentIntentId is not null &&
                record.Status != PaymentReconciliationStatus.Resolved)
            .Select(record => record.PaymentIntentId!.Value)
            .ToHashSet();

        var dtos = intents
            .Where(intent =>
                intent.Status is PaymentIntentStatus.Succeeded or
                    PaymentIntentStatus.PartiallyRefunded or
                    PaymentIntentStatus.Refunded)
            .Select(intent => PaymentMapping.ToSettlementDto(
                intent,
                intent.ProviderCode,
                holdIds.Contains(intent.Id)))
            .ToList();

        return Result<IReadOnlyList<SchoolSettlementPaymentDto>>.Success(dtos);
    }
}
