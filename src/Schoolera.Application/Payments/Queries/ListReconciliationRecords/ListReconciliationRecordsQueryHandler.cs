using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Queries.ListReconciliationRecords;

public sealed record ListReconciliationRecordsQuery(int? Status)
    : IRequest<Result<IReadOnlyList<ReconciliationRecordDto>>>;

public sealed class ListReconciliationRecordsQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<ListReconciliationRecordsQuery, Result<IReadOnlyList<ReconciliationRecordDto>>>
{
    public async Task<Result<IReadOnlyList<ReconciliationRecordDto>>> Handle(
        ListReconciliationRecordsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<ReconciliationRecordDto>>.Failure(
                ["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        PaymentReconciliationStatus? status = null;
        if (request.Status is { } raw && Enum.IsDefined(typeof(PaymentReconciliationStatus), raw))
        {
            status = (PaymentReconciliationStatus)raw;
        }

        var records = await paymentRepository.ListReconciliationAsync(status, cancellationToken);
        return Result<IReadOnlyList<ReconciliationRecordDto>>.Success(
            records.Select(PaymentMapping.ToReconciliationDto).ToList());
    }
}
