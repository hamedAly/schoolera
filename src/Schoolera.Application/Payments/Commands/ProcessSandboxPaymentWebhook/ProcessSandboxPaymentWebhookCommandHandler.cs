using System.Globalization;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Payments.Providers;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Commands.ProcessSandboxPaymentWebhook;

public sealed record ProcessSandboxPaymentWebhookCommand(
    string RawBody,
    string? SignatureHex,
    string? TimestampUnix)
    : IRequest<Result<SandboxWebhookAckDto>>;

public sealed class ProcessSandboxPaymentWebhookCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentProviderResolver providerResolver,
    IPaymentReferenceGenerator referenceGenerator,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<ProcessSandboxPaymentWebhookCommandHandler> logger)
    : IRequestHandler<ProcessSandboxPaymentWebhookCommand, Result<SandboxWebhookAckDto>>
{
    public async Task<Result<SandboxWebhookAckDto>> Handle(
        ProcessSandboxPaymentWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var payload = SchooleraSandboxPaymentProvider.TryParsePayload(request.RawBody);
        if (payload is null || string.IsNullOrWhiteSpace(payload.EventId))
        {
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(false, PaymentErrorCodes.WebhookRejected));
        }

        var existingEvent = await paymentRepository.GetProviderEventAsync(
            payload.EventId.Trim(), cancellationToken);
        if (existingEvent is not null)
        {
            return Result<SandboxWebhookAckDto>.Success(new SandboxWebhookAckDto(true, "replay"));
        }

        PaymentIntent? intent = null;
        if (!string.IsNullOrWhiteSpace(payload.IntentReference))
        {
            intent = await paymentRepository.GetIntentByReferenceAsync(
                payload.IntentReference.Trim(), cancellationToken);
        }

        if (intent is null && !string.IsNullOrWhiteSpace(payload.CheckoutSessionId))
        {
            intent = await paymentRepository.GetIntentByCheckoutSessionAsync(
                payload.CheckoutSessionId.Trim(), cancellationToken);
        }

        if (intent is null)
        {
            // Do not reveal existence details.
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(true, "ignored"));
        }

        var integrationEntity = await notificationRepository.GetIntegrationAsync(
            intent.IntegrationConfigurationId, cancellationToken);
        if (integrationEntity is null ||
            !string.Equals(
                integrationEntity.ProviderCode,
                IntegrationProviderCodes.SchooleraSandbox,
                StringComparison.OrdinalIgnoreCase))
        {
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(false, PaymentErrorCodes.WebhookRejected));
        }

        var settings = IntegrationSettingsSerializer.Deserialize<PaymentIntegrationSettings>(
            integrationEntity.SettingsJson);
        var provider = providerResolver.ResolvePayment(IntegrationProviderCodes.SchooleraSandbox);
        if (provider is null ||
            string.IsNullOrWhiteSpace(settings.WebhookSecret) ||
            !provider.VerifyWebhookSignature(
                request.RawBody,
                request.SignatureHex,
                request.TimestampUnix,
                settings.WebhookSecret))
        {
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(false, PaymentErrorCodes.WebhookRejected));
        }

        if (payload.Amount is { } amount && amount != intent.Amount)
        {
            await paymentRepository.AddReconciliationAsync(
                new PaymentReconciliationRecord(
                    intent.Id,
                    null,
                    intent.ProviderCode,
                    intent.Environment,
                    PaymentReconciliationMismatchType.AmountMismatch,
                    intent.Status.ToString(),
                    payload.Status,
                    intent.Amount,
                    amount,
                    intent.CurrencyCode),
                cancellationToken);
            await paymentRepository.AddProviderEventAsync(
                new PaymentProviderEvent(
                    intent.Id, null, payload.EventId.Trim(), payload.EventType ?? "amount_mismatch",
                    true, "Amount mismatch"),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(true, "amount_mismatch"));
        }

        if (!string.IsNullOrWhiteSpace(payload.CurrencyCode) &&
            !string.Equals(payload.CurrencyCode, intent.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            await paymentRepository.AddReconciliationAsync(
                new PaymentReconciliationRecord(
                    intent.Id,
                    null,
                    intent.ProviderCode,
                    intent.Environment,
                    PaymentReconciliationMismatchType.CurrencyMismatch,
                    intent.Status.ToString(),
                    payload.Status,
                    intent.Amount,
                    payload.Amount,
                    intent.CurrencyCode),
                cancellationToken);
            await paymentRepository.AddProviderEventAsync(
                new PaymentProviderEvent(
                    intent.Id, null, payload.EventId.Trim(), payload.EventType ?? "currency_mismatch",
                    true, "Currency mismatch"),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(true, "currency_mismatch"));
        }

        var mapped = MapStatus(payload.Status);
        var utcNow = DateTimeOffset.UtcNow;

        if (mapped is null)
        {
            await paymentRepository.AddProviderEventAsync(
                new PaymentProviderEvent(
                    intent.Id, null, payload.EventId.Trim(), payload.EventType ?? "unknown",
                    true, "Unknown status"),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<SandboxWebhookAckDto>.Success(new SandboxWebhookAckDto(true, "ignored"));
        }

        if (!intent.TryApplyVerifiedStatus(mapped.Value, utcNow, mapped == PaymentIntentStatus.Failed
                ? "payments.providerFailed"
                : null))
        {
            // Terminal regression rejected — acknowledge without changing state.
            await paymentRepository.AddProviderEventAsync(
                new PaymentProviderEvent(
                    intent.Id, null, payload.EventId.Trim(), payload.EventType ?? "rejected_transition",
                    true, "Invalid transition"),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(true, "invalid_transition"));
        }

        if (!string.IsNullOrWhiteSpace(payload.PaymentReference))
        {
            intent.AttachProviderPaymentReference(payload.PaymentReference);
        }

        if (mapped == PaymentIntentStatus.Succeeded)
        {
            intent.AddTransaction(
                PaymentTransactionType.Charge,
                intent.Amount,
                intent.CurrencyCode,
                payload.PaymentReference,
                succeeded: true);

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
                        integrationEntity.DisplayNameAr,
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
        else if (mapped == PaymentIntentStatus.Failed)
        {
            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                intent.ParentUserId,
                intent.Id,
                NotificationEventType.PaymentFailed,
                "failed",
                intent.Reference,
                $"/parent/payments/{intent.Id}",
                intent.SchoolId,
                cancellationToken);
        }
        else if (mapped == PaymentIntentStatus.Cancelled)
        {
            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                intent.ParentUserId,
                intent.Id,
                NotificationEventType.PaymentCancelled,
                "cancelled",
                intent.Reference,
                $"/parent/payments/{intent.Id}",
                intent.SchoolId,
                cancellationToken);
        }

        await paymentRepository.AddProviderEventAsync(
            new PaymentProviderEvent(
                intent.Id,
                null,
                payload.EventId.Trim(),
                payload.EventType ?? mapped.Value.ToString(),
                true,
                $"Status {mapped}"),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Sandbox payment webhook processed for intent {Reference} status {Status}.",
            intent.Reference,
            mapped);

        return Result<SandboxWebhookAckDto>.Success(new SandboxWebhookAckDto(true, "ok"));
    }

    private static PaymentIntentStatus? MapStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "succeeded" or "success" or "paid" => PaymentIntentStatus.Succeeded,
            "failed" or "failure" => PaymentIntentStatus.Failed,
            "cancelled" or "canceled" => PaymentIntentStatus.Cancelled,
            "expired" => PaymentIntentStatus.Expired,
            "processing" or "pending" => PaymentIntentStatus.Processing,
            _ => null,
        };
    }
}
