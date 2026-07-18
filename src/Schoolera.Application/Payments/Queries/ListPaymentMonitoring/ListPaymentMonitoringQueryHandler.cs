using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Queries.ListPaymentMonitoring;

public sealed record ListPaymentMonitoringQuery(int? Status, int Take = 100)
    : IRequest<Result<IReadOnlyList<AdminPaymentMonitoringDto>>>;

public sealed class ListPaymentMonitoringQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<ListPaymentMonitoringQuery, Result<IReadOnlyList<AdminPaymentMonitoringDto>>>
{
    public async Task<Result<IReadOnlyList<AdminPaymentMonitoringDto>>> Handle(
        ListPaymentMonitoringQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<AdminPaymentMonitoringDto>>.Failure(
                ["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        PaymentIntentStatus? status = null;
        if (request.Status is { } raw && Enum.IsDefined(typeof(PaymentIntentStatus), raw))
        {
            status = (PaymentIntentStatus)raw;
        }

        var intents = await paymentRepository.ListIntentsForAdminAsync(
            status, request.Take, cancellationToken);
        return Result<IReadOnlyList<AdminPaymentMonitoringDto>>.Success(
            intents.Select(PaymentMapping.ToMonitoringDto).ToList());
    }
}
