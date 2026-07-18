using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Commands.RequestPaymentRefund;

public sealed record RequestPaymentRefundCommand(Guid IntentId, RequestPaymentRefundRequest Body)
    : IRequest<Result<PaymentIntentDto>>;

public sealed class RequestPaymentRefundCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    IPaymentProviderResolver providerResolver,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<RequestPaymentRefundCommandHandler> logger)
    : IRequestHandler<RequestPaymentRefundCommand, Result<PaymentIntentDto>>
{
    public async Task<Result<PaymentIntentDto>> Handle(
        RequestPaymentRefundCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<PaymentIntentDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body.IdempotencyKey) || body.IdempotencyKey.Length > 128)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Invalid idempotency key."],
                [PaymentErrorCodes.ValidationFailed]);
        }

        if (string.IsNullOrWhiteSpace(body.ReasonCode))
        {
            return Result<PaymentIntentDto>.Failure(
                ["Validation failed."],
                [PaymentErrorCodes.ValidationFailed]);
        }

        var intent = await paymentRepository.GetIntentByIdAsync(request.IntentId, cancellationToken);
        if (intent is null)
        {
            return Result<PaymentIntentDto>.Failure(["Not found."], [PaymentErrorCodes.NotFound]);
        }

        if (intent.Status is not (PaymentIntentStatus.Succeeded or PaymentIntentStatus.PartiallyRefunded))
        {
            return Result<PaymentIntentDto>.Failure(
                ["Refund not supported."],
                [PaymentErrorCodes.RefundNotSupported]);
        }

        var alreadyRefunded = await paymentRepository.SumSuccessfulRefundsAsync(
            intent.Id, cancellationToken);
        var remaining = intent.Amount - alreadyRefunded;
        if (remaining <= 0m)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Refund exceeds paid amount."],
                [PaymentErrorCodes.RefundExceedsPaid]);
        }

        var refundAmount = body.Amount ?? remaining;
        if (refundAmount <= 0m || refundAmount > remaining)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Refund exceeds paid amount."],
                [PaymentErrorCodes.RefundExceedsPaid]);
        }

        var integration = await providerResolver.ResolveActivePaymentIntegrationAsync(
            intent.IntegrationConfigurationId, cancellationToken);
        if (integration is null)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Payment provider not configured."],
                [PaymentErrorCodes.ProviderNotConfigured]);
        }

        var provider = providerResolver.ResolvePayment(integration.ProviderCode);
        if (provider is null)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Refund not supported."],
                [PaymentErrorCodes.RefundNotSupported]);
        }

        var refund = await provider.RefundAsync(
            intent, refundAmount, body.IdempotencyKey.Trim(), integration, cancellationToken);
        if (!refund.Succeeded)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Refund not supported."],
                [refund.SafeFailureCode ?? PaymentErrorCodes.RefundNotSupported]);
        }

        intent.AddTransaction(
            PaymentTransactionType.Refund,
            refundAmount,
            intent.CurrencyCode,
            refund.ProviderRefundId,
            succeeded: true);

        var nextStatus = refundAmount >= remaining
            ? PaymentIntentStatus.Refunded
            : PaymentIntentStatus.PartiallyRefunded;
        intent.TryApplyVerifiedStatus(nextStatus, DateTimeOffset.UtcNow);

        await PaymentNotificationSupport.EnqueueAsync(
            notificationPublisher,
            parentAccountService,
            intent.ParentUserId,
            intent.Id,
            NotificationEventType.PaymentRefundCompleted,
            "refunded",
            intent.Reference,
            $"/parent/payments/{intent.Id}",
            intent.SchoolId,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            PaymentAuditActions.RefundRequested,
            "PaymentIntent",
            intent.Id.ToString(),
            $"Refund {refundAmount} {intent.CurrencyCode} reason {body.ReasonCode}.",
            cancellationToken);

        logger.LogInformation(
            "Refund {Amount} applied to intent {Reference} by admin {ActorId}.",
            refundAmount,
            intent.Reference,
            actorId);

        return Result<PaymentIntentDto>.Success(
            PaymentMapping.ToIntentDto(
                intent,
                integration.DisplayNameAr,
                integration.DisplayNameEn));
    }
}
