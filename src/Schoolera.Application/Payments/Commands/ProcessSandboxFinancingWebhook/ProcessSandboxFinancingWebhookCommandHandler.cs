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

namespace Schoolera.Application.Payments.Commands.ProcessSandboxFinancingWebhook;

public sealed record ProcessSandboxFinancingWebhookCommand(
    string RawBody,
    string? SignatureHex,
    string? TimestampUnix)
    : IRequest<Result<SandboxWebhookAckDto>>;

public sealed class ProcessSandboxFinancingWebhookCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentProviderResolver providerResolver,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<ProcessSandboxFinancingWebhookCommandHandler> logger)
    : IRequestHandler<ProcessSandboxFinancingWebhookCommand, Result<SandboxWebhookAckDto>>
{
    public async Task<Result<SandboxWebhookAckDto>> Handle(
        ProcessSandboxFinancingWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var payload = SandboxFinancingWebhookPayload.TryParse(request.RawBody);
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

        if (string.IsNullOrWhiteSpace(payload.RequestReference))
        {
            return Result<SandboxWebhookAckDto>.Success(new SandboxWebhookAckDto(true, "ignored"));
        }

        var financing = await paymentRepository.GetFinancingByReferenceAsync(
            payload.RequestReference.Trim(), cancellationToken);
        if (financing is null)
        {
            return Result<SandboxWebhookAckDto>.Success(new SandboxWebhookAckDto(true, "ignored"));
        }

        var integrationEntity = await notificationRepository.GetIntegrationAsync(
            financing.IntegrationConfigurationId, cancellationToken);
        if (integrationEntity is null ||
            !string.Equals(
                integrationEntity.ProviderCode,
                IntegrationProviderCodes.SchooleraSandbox,
                StringComparison.OrdinalIgnoreCase))
        {
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(false, PaymentErrorCodes.WebhookRejected));
        }

        var settings = IntegrationSettingsSerializer.Deserialize<FinancingIntegrationSettings>(
            integrationEntity.SettingsJson);
        var provider = providerResolver.ResolveFinancing(IntegrationProviderCodes.SchooleraSandbox);
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

        var mapped = MapStatus(payload.Status);
        if (mapped is null)
        {
            await paymentRepository.AddProviderEventAsync(
                new PaymentProviderEvent(
                    null, financing.Id, payload.EventId.Trim(), payload.EventType ?? "unknown",
                    true, "Unknown status"),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<SandboxWebhookAckDto>.Success(new SandboxWebhookAckDto(true, "ignored"));
        }

        // Approved must never be treated as Funded.
        if (!financing.TryApplyStatus(mapped.Value))
        {
            await paymentRepository.AddProviderEventAsync(
                new PaymentProviderEvent(
                    null, financing.Id, payload.EventId.Trim(), payload.EventType ?? "rejected_transition",
                    true, "Invalid transition"),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<SandboxWebhookAckDto>.Success(
                new SandboxWebhookAckDto(true, "invalid_transition"));
        }

        financing.AddDecision(mapped.Value.ToString(), $"Webhook status {mapped}");

        if (mapped == FinancingRequestStatus.Approved)
        {
            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                financing.ParentUserId,
                financing.Id,
                NotificationEventType.FinancingApproved,
                "approved",
                financing.Reference,
                $"/parent/financing/{financing.Id}",
                financing.SchoolId,
                cancellationToken);
        }
        else if (mapped == FinancingRequestStatus.Declined)
        {
            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                financing.ParentUserId,
                financing.Id,
                NotificationEventType.FinancingDeclined,
                "declined",
                financing.Reference,
                $"/parent/financing/{financing.Id}",
                financing.SchoolId,
                cancellationToken);
        }
        else if (mapped == FinancingRequestStatus.Funded)
        {
            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                financing.ParentUserId,
                financing.Id,
                NotificationEventType.FinancingFunded,
                "funded",
                financing.Reference,
                $"/parent/financing/{financing.Id}",
                financing.SchoolId,
                cancellationToken);
        }
        else if (mapped == FinancingRequestStatus.Expired)
        {
            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                financing.ParentUserId,
                financing.Id,
                NotificationEventType.FinancingOfferExpired,
                "expired",
                financing.Reference,
                $"/parent/financing/{financing.Id}",
                financing.SchoolId,
                cancellationToken);
        }

        await paymentRepository.AddProviderEventAsync(
            new PaymentProviderEvent(
                null,
                financing.Id,
                payload.EventId.Trim(),
                payload.EventType ?? mapped.Value.ToString(),
                true,
                $"Status {mapped}"),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Sandbox financing webhook processed for {Reference} status {Status}.",
            financing.Reference,
            mapped);

        return Result<SandboxWebhookAckDto>.Success(new SandboxWebhookAckDto(true, "ok"));
    }

    private static FinancingRequestStatus? MapStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "offers_available" or "offersavailable" => FinancingRequestStatus.OffersAvailable,
            "offer_selected" or "offerselected" => FinancingRequestStatus.OfferSelected,
            "provider_review" or "providerreview" or "review" => FinancingRequestStatus.ProviderReview,
            "approved" => FinancingRequestStatus.Approved,
            "declined" or "rejected" => FinancingRequestStatus.Declined,
            "funding_pending" or "fundingpending" => FinancingRequestStatus.FundingPending,
            "funded" => FinancingRequestStatus.Funded,
            "expired" => FinancingRequestStatus.Expired,
            "cancelled" or "canceled" => FinancingRequestStatus.Cancelled,
            _ => null,
        };
    }
}
