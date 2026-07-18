using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Queries.ListParentPayableItems;

public sealed record ListParentPayableItemsQuery : IRequest<Result<IReadOnlyList<ParentPayableItemDto>>>;

public sealed class ListParentPayableItemsQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    ISchoolPortalRepository schoolPortalRepository)
    : IRequestHandler<ListParentPayableItemsQuery, Result<IReadOnlyList<ParentPayableItemDto>>>
{
    public async Task<Result<IReadOnlyList<ParentPayableItemDto>>> Handle(
        ListParentPayableItemsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<IReadOnlyList<ParentPayableItemDto>>.Failure(
                ["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var items = await paymentRepository.ListActivePayableItemsAsync(cancellationToken);
        var results = new List<ParentPayableItemDto>();
        var schoolCache = new Dictionary<Guid, (string NameAr, string? NameEn)>();

        foreach (var item in items)
        {
            var fee = item.TuitionFee;
            if (fee is null || !fee.IsActive || !fee.IsPublished || fee.Amount <= 0m)
            {
                continue;
            }

            if (!schoolCache.TryGetValue(item.SchoolId, out var schoolNames))
            {
                var school = await schoolPortalRepository.GetSchoolProfileAsync(
                    item.SchoolId, cancellationToken);
                schoolNames = (school?.NameAr ?? string.Empty, school?.NameEn);
                schoolCache[item.SchoolId] = schoolNames;
            }

            var alreadyPaid = await paymentRepository.HasSucceededPaymentForPayableAsync(
                item.Id, parentId, cancellationToken);
            var hasActive = await paymentRepository.HasActiveIntentForPayableAsync(
                item.Id, parentId, cancellationToken);

            results.Add(new ParentPayableItemDto(
                item.Id,
                item.SchoolId,
                item.SchoolBranchId,
                fee.Id,
                schoolNames.NameAr,
                schoolNames.NameEn,
                fee.NameAr ?? fee.Category.ToString(),
                fee.NameEn,
                fee.Category.ToString(),
                fee.Amount,
                fee.CurrencyCode,
                item.PaymentInstructionsAr,
                item.PaymentInstructionsEn,
                item.IsCurrentlyPayable(DateTimeOffset.UtcNow),
                alreadyPaid,
                hasActive));
        }

        return Result<IReadOnlyList<ParentPayableItemDto>>.Success(results);
    }
}
