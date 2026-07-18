using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Queries.ListParentPaymentIntents;

public sealed record ListParentPaymentIntentsQuery : IRequest<Result<IReadOnlyList<PaymentIntentDto>>>;

public sealed class ListParentPaymentIntentsQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<ListParentPaymentIntentsQuery, Result<IReadOnlyList<PaymentIntentDto>>>
{
    public async Task<Result<IReadOnlyList<PaymentIntentDto>>> Handle(
        ListParentPaymentIntentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<IReadOnlyList<PaymentIntentDto>>.Failure(
                ["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var intents = await paymentRepository.ListIntentsForParentAsync(parentId, cancellationToken);
        var dtos = intents
            .Select(intent => PaymentMapping.ToIntentDto(intent, intent.ProviderCode, null))
            .ToList();
        return Result<IReadOnlyList<PaymentIntentDto>>.Success(dtos);
    }
}
