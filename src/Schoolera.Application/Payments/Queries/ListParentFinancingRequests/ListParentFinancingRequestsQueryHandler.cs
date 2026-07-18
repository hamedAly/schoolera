using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Queries.ListParentFinancingRequests;

public sealed record ListParentFinancingRequestsQuery
    : IRequest<Result<IReadOnlyList<FinancingRequestDto>>>;

public sealed class ListParentFinancingRequestsQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<ListParentFinancingRequestsQuery, Result<IReadOnlyList<FinancingRequestDto>>>
{
    public async Task<Result<IReadOnlyList<FinancingRequestDto>>> Handle(
        ListParentFinancingRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<IReadOnlyList<FinancingRequestDto>>.Failure(
                ["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var items = await paymentRepository.ListFinancingForParentAsync(parentId, cancellationToken);
        return Result<IReadOnlyList<FinancingRequestDto>>.Success(
            items.Select(PaymentMapping.ToFinancingDto).ToList());
    }
}
