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

namespace Schoolera.Application.Payments.Commands.CreatePaymentIntent;

public sealed record CreatePaymentIntentCommand(CreatePaymentIntentRequest Body)
    : IRequest<Result<PaymentIntentDto>>;

public sealed class CreatePaymentIntentCommandHandler(
    ICurrentUser currentUser,
    IPayableAmountResolver payableResolver,
    IPaymentRepository paymentRepository,
    IPaymentReferenceGenerator referenceGenerator,
    IPaymentProviderResolver providerResolver,
    ILegalConsentService legalConsentService,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<CreatePaymentIntentCommandHandler> logger)
    : IRequestHandler<CreatePaymentIntentCommand, Result<PaymentIntentDto>>
{
    public async Task<Result<PaymentIntentDto>> Handle(
        CreatePaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
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

        if (!Enum.IsDefined(body.PaymentMethod))
        {
            return Result<PaymentIntentDto>.Failure(
                ["Invalid payment method."],
                [PaymentErrorCodes.ValidationFailed]);
        }

        var existing = await paymentRepository.GetIntentByIdempotencyAsync(
            parentId, body.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            if (existing.PayableItemId != body.PayableItemId)
            {
                return Result<PaymentIntentDto>.Failure(
                    ["Idempotency key conflict."],
                    [PaymentErrorCodes.IdempotencyConflict]);
            }

            return Result<PaymentIntentDto>.Success(
                PaymentMapping.ToIntentDto(existing, existing.ProviderCode, null));
        }

        if (!body.TermsAccepted || !body.PrivacyAccepted)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Consent required."],
                [PaymentErrorCodes.ConsentRequired]);
        }

        var consent = await legalConsentService.PersistCurrentAcceptancesAsync(
            parentId,
            LegalAcceptancePurpose.PaymentInitiation,
            body.TermsAccepted,
            body.PrivacyAccepted,
            cancellationToken);
        if (!consent.Succeeded)
        {
            return Result<PaymentIntentDto>.Failure(
                consent.Errors,
                consent.ErrorCodes.Count > 0 ? consent.ErrorCodes : [PaymentErrorCodes.ConsentRequired]);
        }

        var payable = await payableResolver.ResolveForParentAsync(
            parentId, body.PayableItemId, body.AdmissionApplicationId, cancellationToken);
        if (payable is null)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Payable item not found."],
                [PaymentErrorCodes.PayableNotFound]);
        }

        if (!payable.IsCurrentlyPayable)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Payable item is not available."],
                [PaymentErrorCodes.PayableNotAvailable]);
        }

        if (payable.AlreadyPaid)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Already paid."],
                [PaymentErrorCodes.AlreadyPaid]);
        }

        if (payable.HasActiveIntent)
        {
            return Result<PaymentIntentDto>.Failure(
                ["Active payment intent exists."],
                [PaymentErrorCodes.ActiveIntentExists]);
        }

        var integration = await providerResolver.ResolveActivePaymentIntegrationAsync(
            body.IntegrationConfigurationId, cancellationToken);
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

        var reference = await referenceGenerator.GeneratePaymentReferenceAsync(cancellationToken);
        var intent = new PaymentIntent(
            reference,
            parentId,
            payable.SchoolId,
            payable.SchoolBranchId,
            payable.PayableItemId,
            payable.AdmissionApplicationId,
            payable.NetPayableAmount,
            payable.CurrencyCode,
            integration.IntegrationId,
            integration.ProviderCode,
            integration.Environment,
            body.PaymentMethod,
            body.IdempotencyKey.Trim(),
            DateTimeOffset.UtcNow.AddHours(1));

        var checkout = await provider.CreateHostedCheckoutAsync(intent, integration, cancellationToken);
        if (!checkout.Succeeded || string.IsNullOrWhiteSpace(checkout.CheckoutSessionId))
        {
            return Result<PaymentIntentDto>.Failure(
                ["Checkout initiation failed."],
                [checkout.SafeFailureCode ?? PaymentErrorCodes.ProviderNotConfigured]);
        }

        intent.MarkPendingProvider(checkout.CheckoutSessionId, checkout.RedirectUrl);
        await paymentRepository.AddIntentAsync(intent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await PaymentNotificationSupport.EnqueueAsync(
            notificationPublisher,
            parentAccountService,
            parentId,
            intent.Id,
            NotificationEventType.PaymentInitiated,
            "initiated",
            intent.Reference,
            $"/parent/payments/{intent.Id}",
            intent.SchoolId,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            parentId,
            PaymentAuditActions.IntentCreated,
            "PaymentIntent",
            intent.Id.ToString(),
            $"Created payment intent {intent.Reference} (sandbox={intent.Environment == ProviderEnvironment.Sandbox}).",
            cancellationToken);

        logger.LogInformation(
            "Payment intent {Reference} created for parent {ParentId} provider {ProviderCode}.",
            intent.Reference,
            parentId,
            intent.ProviderCode);

        return Result<PaymentIntentDto>.Success(
            PaymentMapping.ToIntentDto(intent, integration.DisplayNameAr, integration.DisplayNameEn));
    }
}
