using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Queries.GetFinancingRequest;

public sealed record GetFinancingRequestQuery(Guid RequestId) : IRequest<Result<FinancingRequestDto>>;

public sealed class GetFinancingRequestQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetFinancingRequestQuery, Result<FinancingRequestDto>>
{
    public async Task<Result<FinancingRequestDto>> Handle(
        GetFinancingRequestQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<FinancingRequestDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var entity = await paymentRepository.GetFinancingByIdAsync(request.RequestId, cancellationToken);
        if (entity is null || entity.ParentUserId != parentId)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Not found."], [PaymentErrorCodes.FinancingNotFound]);
        }

        return Result<FinancingRequestDto>.Success(PaymentMapping.ToFinancingDto(entity));
    }
}
