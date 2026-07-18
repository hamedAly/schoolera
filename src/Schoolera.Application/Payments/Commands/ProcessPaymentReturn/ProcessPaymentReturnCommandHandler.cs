using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Commands.ProcessPaymentReturn;

public sealed record ProcessPaymentReturnCommand(string Reference)
    : IRequest<Result<PaymentIntentDto>>;

/// <summary>
/// Parent return from hosted checkout. Marks returned/processing only — never Succeeded.
/// </summary>
public sealed class ProcessPaymentReturnCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    IPaymentProviderResolver providerResolver,
    IUnitOfWork unitOfWork,
    ILogger<ProcessPaymentReturnCommandHandler> logger)
    : IRequestHandler<ProcessPaymentReturnCommand, Result<PaymentIntentDto>>
{
    public async Task<Result<PaymentIntentDto>> Handle(
        ProcessPaymentReturnCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<PaymentIntentDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            return Result<PaymentIntentDto>.Failure(["Not found."], [PaymentErrorCodes.NotFound]);
        }

        var intent = await paymentRepository.GetIntentByReferenceAsync(
            request.Reference.Trim(), cancellationToken);
        if (intent is null || intent.ParentUserId != parentId)
        {
            return Result<PaymentIntentDto>.Failure(["Not found."], [PaymentErrorCodes.NotFound]);
        }

        if (intent.ExpiresAtUtc <= DateTimeOffset.UtcNow &&
            intent.Status is PaymentIntentStatus.Created or
                PaymentIntentStatus.PendingProvider or
                PaymentIntentStatus.RequiresParentAction)
        {
            intent.TryApplyVerifiedStatus(PaymentIntentStatus.Expired, DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            intent.MarkReturned();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var integration = await providerResolver.ResolveActivePaymentIntegrationAsync(
            intent.IntegrationConfigurationId, cancellationToken);

        logger.LogInformation("Payment return processed for {Reference} status {Status}.", intent.Reference, intent.Status);

        return Result<PaymentIntentDto>.Success(
            PaymentMapping.ToIntentDto(
                intent,
                integration?.DisplayNameAr ?? intent.ProviderCode,
                integration?.DisplayNameEn));
    }
}
