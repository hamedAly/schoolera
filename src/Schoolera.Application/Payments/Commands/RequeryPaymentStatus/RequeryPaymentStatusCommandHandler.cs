using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Commands.RequeryPaymentStatus;

public sealed record RequeryPaymentStatusCommand(Guid IntentId) : IRequest<Result<PaymentIntentDto>>;

public sealed class RequeryPaymentStatusCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    IPaymentProviderResolver providerResolver,
    IPaymentReferenceGenerator referenceGenerator,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<RequeryPaymentStatusCommandHandler> logger)
    : IRequestHandler<RequeryPaymentStatusCommand, Result<PaymentIntentDto>>
{
    public async Task<Result<PaymentIntentDto>> Handle(
        RequeryPaymentStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<PaymentIntentDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var intent = await paymentRepository.GetIntentByIdAsync(request.IntentId, cancellationToken);
        if (intent is null)
        {
            return Result<PaymentIntentDto>.Failure(["Not found."], [PaymentErrorCodes.NotFound]);
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
                ["Payment provider not configured."],
                [PaymentErrorCodes.ProviderNotConfigured]);
        }

        var statusResult = await provider.QueryStatusAsync(intent, integration, cancellationToken);
        if (!statusResult.Succeeded || statusResult.MappedStatus is null)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Provider status query failed."],
                [statusResult.SafeFailureCode ?? PaymentErrorCodes.ProviderNotConfigured]);
        }

        var mapped = statusResult.MappedStatus.Value;
        var utcNow = DateTimeOffset.UtcNow;
        if (!intent.TryApplyVerifiedStatus(
                mapped,
                utcNow,
                mapped == PaymentIntentStatus.Failed ? "payments.providerFailed" : null))
        {
            return Result<PaymentIntentDto>.Failure(
                ["Invalid transition."],
                [PaymentErrorCodes.InvalidTransition]);
        }

        if (!string.IsNullOrWhiteSpace(statusResult.ProviderPaymentReference))
        {
            intent.AttachProviderPaymentReference(statusResult.ProviderPaymentReference);
        }

        if (mapped == PaymentIntentStatus.Succeeded)
        {
            var existingReceipt = await paymentRepository.GetReceiptByIntentIdAsync(
                intent.Id, cancellationToken);
            if (existingReceipt is null)
            {
                var receiptNumber = await referenceGenerator.GenerateReceiptNumberAsync(cancellationToken);
                await paymentRepository.AddReceiptAsync(
                    new PaymentReceipt(
                        intent.Id,
                        receiptNumber,
                        intent.ParentUserId,
                        intent.SchoolId,
                        intent.Amount,
                        intent.CurrencyCode,
                        intent.Environment,
                        integration.DisplayNameAr,
                        intent.ProviderPaymentReference,
                        utcNow),
                    cancellationToken);
            }

            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                intent.ParentUserId,
                intent.Id,
                NotificationEventType.PaymentSucceeded,
                "succeeded",
                intent.Reference,
                $"/parent/payments/{intent.Id}/receipt",
                intent.SchoolId,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            PaymentAuditActions.StatusRequeried,
            "PaymentIntent",
            intent.Id.ToString(),
            $"Requeried status to {mapped}.",
            cancellationToken);

        logger.LogInformation(
            "Admin {ActorId} requeried payment {Reference} to {Status}.",
            actorId,
            intent.Reference,
            mapped);

        return Result<PaymentIntentDto>.Success(
            PaymentMapping.ToIntentDto(
                intent,
                integration.DisplayNameAr,
                integration.DisplayNameEn));
    }
}
