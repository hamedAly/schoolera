using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Commands.SelectFinancingOffer;

public sealed record SelectFinancingOfferCommand(Guid RequestId, SelectFinancingOfferRequest Body)
    : IRequest<Result<FinancingRequestDto>>;

public sealed class SelectFinancingOfferCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<SelectFinancingOfferCommandHandler> logger)
    : IRequestHandler<SelectFinancingOfferCommand, Result<FinancingRequestDto>>
{
    public async Task<Result<FinancingRequestDto>> Handle(
        SelectFinancingOfferCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<FinancingRequestDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        if (!request.Body.ConsentAccepted)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Consent required."],
                [PaymentErrorCodes.ConsentRequired]);
        }

        var financing = await paymentRepository.GetFinancingByIdAsync(request.RequestId, cancellationToken);
        if (financing is null || financing.ParentUserId != parentId)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Not found."], [PaymentErrorCodes.FinancingNotFound]);
        }

        var offer = financing.Offers.FirstOrDefault(item => item.Id == request.Body.OfferId);
        if (offer is null)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Offer not available."],
                [PaymentErrorCodes.OfferNotAvailable]);
        }

        if (offer.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Offer expired."],
                [PaymentErrorCodes.OfferExpired]);
        }

        if (!financing.TrySelectOffer(request.Body.OfferId, DateTimeOffset.UtcNow))
        {
            return Result<FinancingRequestDto>.Failure(
                ["Offer not available."],
                [PaymentErrorCodes.OfferNotAvailable]);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            parentId,
            PaymentAuditActions.OfferSelected,
            "FinancingRequest",
            financing.Id.ToString(),
            $"Selected offer {request.Body.OfferId} on {financing.Reference}.",
            cancellationToken);

        logger.LogInformation(
            "Financing offer {OfferId} selected on request {Reference}.",
            request.Body.OfferId,
            financing.Reference);

        return Result<FinancingRequestDto>.Success(PaymentMapping.ToFinancingDto(financing));
    }
}
