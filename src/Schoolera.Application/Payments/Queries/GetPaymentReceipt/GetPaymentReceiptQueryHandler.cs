using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Queries.GetPaymentReceipt;

public sealed record GetPaymentReceiptQuery(Guid IntentId) : IRequest<Result<PaymentReceiptDto>>;

public sealed class GetPaymentReceiptQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentReceiptQuery, Result<PaymentReceiptDto>>
{
    public async Task<Result<PaymentReceiptDto>> Handle(
        GetPaymentReceiptQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<PaymentReceiptDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var intent = await paymentRepository.GetIntentByIdAsync(request.IntentId, cancellationToken);
        if (intent is null || intent.ParentUserId != parentId)
        {
            return Result<PaymentReceiptDto>.Failure(["Not found."], [PaymentErrorCodes.NotFound]);
        }

        var receipt = await paymentRepository.GetReceiptByIntentIdAsync(intent.Id, cancellationToken);
        if (receipt is null)
        {
            return Result<PaymentReceiptDto>.Failure(
                ["Receipt not found."],
                [PaymentErrorCodes.ReceiptNotFound]);
        }

        return Result<PaymentReceiptDto>.Success(PaymentMapping.ToReceiptDto(receipt, intent));
    }
}
