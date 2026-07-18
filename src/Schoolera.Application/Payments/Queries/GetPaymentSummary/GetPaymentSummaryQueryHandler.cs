using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Queries.GetPaymentSummary;

public sealed record GetPaymentSummaryQuery(Guid PayableItemId, Guid? AdmissionApplicationId)
    : IRequest<Result<PaymentSummaryDto>>;

public sealed class GetPaymentSummaryQueryHandler(
    ICurrentUser currentUser,
    IPayableAmountResolver payableResolver,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetPaymentSummaryQuery, Result<PaymentSummaryDto>>
{
    public async Task<Result<PaymentSummaryDto>> Handle(
        GetPaymentSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<PaymentSummaryDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var payable = await payableResolver.ResolveForParentAsync(
            parentId, request.PayableItemId, request.AdmissionApplicationId, cancellationToken);
        if (payable is null)
        {
            return Result<PaymentSummaryDto>.Failure(
                ["Payable item not found."],
                [PaymentErrorCodes.PayableNotFound]);
        }

        var integrations = await notificationRepository.ListIntegrationsAsync(
            IntegrationType.Payment, null, true, null, cancellationToken);
        var providers = integrations
            .Select(entity =>
            {
                var settings = IntegrationSettingsSerializer.Deserialize<PaymentIntegrationSettings>(
                    entity.SettingsJson);
                return PaymentMapping.ToPublicProviderDto(entity, settings, entity.IsDefault);
            })
            .ToList();

        return Result<PaymentSummaryDto>.Success(new PaymentSummaryDto(
            payable.PayableItemId,
            payable.SchoolId,
            payable.SchoolBranchId,
            payable.AdmissionApplicationId,
            payable.FeeCategory.ToString(),
            payable.FeeNameAr,
            payable.FeeNameEn,
            payable.GrossAmount,
            payable.DiscountAmount,
            payable.NetPayableAmount,
            payable.CurrencyCode,
            payable.IsCurrentlyPayable,
            payable.AlreadyPaid,
            payable.HasActiveIntent,
            payable.PaymentInstructionsAr,
            payable.PaymentInstructionsEn,
            providers));
    }
}
