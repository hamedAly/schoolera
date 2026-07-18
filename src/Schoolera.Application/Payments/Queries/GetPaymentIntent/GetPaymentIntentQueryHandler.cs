using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Queries.GetPaymentIntent;

public sealed record GetPaymentIntentQuery(Guid IntentId) : IRequest<Result<PaymentIntentDto>>;

public sealed class GetPaymentIntentQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    IPaymentProviderResolver providerResolver)
    : IRequestHandler<GetPaymentIntentQuery, Result<PaymentIntentDto>>
{
    public async Task<Result<PaymentIntentDto>> Handle(
        GetPaymentIntentQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<PaymentIntentDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var intent = await paymentRepository.GetIntentByIdAsync(request.IntentId, cancellationToken);
        if (intent is null || intent.ParentUserId != parentId)
        {
            return Result<PaymentIntentDto>.Failure(["Not found."], [PaymentErrorCodes.NotFound]);
        }

        var integration = await providerResolver.ResolveActivePaymentIntegrationAsync(
            intent.IntegrationConfigurationId, cancellationToken);
        return Result<PaymentIntentDto>.Success(
            PaymentMapping.ToIntentDto(
                intent,
                integration?.DisplayNameAr ?? intent.ProviderCode,
                integration?.DisplayNameEn));
    }
}
