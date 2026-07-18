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

namespace Schoolera.Application.Payments.Commands.CreateFinancingRequest;

public sealed record CreateFinancingRequestCommand(CreateFinancingRequestBody Body)
    : IRequest<Result<FinancingRequestDto>>;

public sealed class CreateFinancingRequestCommandHandler(
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
    ILogger<CreateFinancingRequestCommandHandler> logger)
    : IRequestHandler<CreateFinancingRequestCommand, Result<FinancingRequestDto>>
{
    public async Task<Result<FinancingRequestDto>> Handle(
        CreateFinancingRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } parentId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<FinancingRequestDto>.Failure(["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body.IdempotencyKey) || body.IdempotencyKey.Length > 128)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Invalid idempotency key."],
                [PaymentErrorCodes.ValidationFailed]);
        }

        var existing = await paymentRepository.GetFinancingByIdempotencyAsync(
            parentId, body.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            if (existing.PayableItemId != body.PayableItemId)
            {
                return Result<FinancingRequestDto>.Failure(
                    ["Idempotency key conflict."],
                    [PaymentErrorCodes.IdempotencyConflict]);
            }

            return Result<FinancingRequestDto>.Success(PaymentMapping.ToFinancingDto(existing));
        }

        if (!body.TermsAccepted || !body.PrivacyAccepted)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Consent required."],
                [PaymentErrorCodes.ConsentRequired]);
        }

        var consent = await legalConsentService.PersistCurrentAcceptancesAsync(
            parentId,
            LegalAcceptancePurpose.FinancingInitiation,
            body.TermsAccepted,
            body.PrivacyAccepted,
            cancellationToken);
        if (!consent.Succeeded)
        {
            return Result<FinancingRequestDto>.Failure(
                consent.Errors,
                consent.ErrorCodes.Count > 0 ? consent.ErrorCodes : [PaymentErrorCodes.ConsentRequired]);
        }

        var payable = await payableResolver.ResolveForParentAsync(
            parentId, body.PayableItemId, body.AdmissionApplicationId, cancellationToken);
        if (payable is null)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Payable item not found."],
                [PaymentErrorCodes.PayableNotFound]);
        }

        if (!payable.IsCurrentlyPayable)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Payable item is not available."],
                [PaymentErrorCodes.PayableNotAvailable]);
        }

        if (payable.AlreadyPaid)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Already paid."],
                [PaymentErrorCodes.AlreadyPaid]);
        }

        var integration = await providerResolver.ResolveActiveFinancingIntegrationAsync(
            body.IntegrationConfigurationId, cancellationToken);
        if (integration is null)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Financing provider not configured."],
                [PaymentErrorCodes.ProviderNotConfigured]);
        }

        var provider = providerResolver.ResolveFinancing(integration.ProviderCode);
        if (provider is null)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Financing provider not configured."],
                [PaymentErrorCodes.ProviderNotConfigured]);
        }

        Guid? consentVersionId = null;
        var reference = await referenceGenerator.GenerateFinancingReferenceAsync(cancellationToken);
        var financing = new FinancingRequest(
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
            body.IdempotencyKey.Trim(),
            consentVersionId);

        var offersResult = await provider.RequestOffersAsync(financing, integration, cancellationToken);
        if (!offersResult.Succeeded)
        {
            return Result<FinancingRequestDto>.Failure(
                ["Financing offer request failed."],
                [offersResult.SafeFailureCode ?? PaymentErrorCodes.ProviderNotConfigured]);
        }

        if (offersResult.Offers.Count == 0)
        {
            financing.TryApplyStatus(FinancingRequestStatus.Declined);
            financing.AddDecision("declined", "No offers returned by provider.");
        }
        else
        {
            foreach (var draft in offersResult.Offers)
            {
                financing.AddOffer(
                    draft.ProviderOfferReference,
                    draft.TenorMonths,
                    draft.PeriodicInstallment,
                    draft.TotalRepayment,
                    draft.Fees,
                    draft.InterestOrProfitRate,
                    draft.AprProviderReported,
                    draft.DownPayment,
                    draft.ExpiresAtUtc,
                    draft.DisclosureText);
            }

            financing.MarkOffersAvailable(offersResult.ProviderRequestReference ?? reference);
        }

        await paymentRepository.AddFinancingAsync(financing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (financing.Status == FinancingRequestStatus.OffersAvailable)
        {
            await PaymentNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                parentId,
                financing.Id,
                NotificationEventType.FinancingOffersAvailable,
                "offers",
                financing.Reference,
                $"/parent/financing/{financing.Id}",
                financing.SchoolId,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await adminPlatform.WriteAuditAsync(
            parentId,
            PaymentAuditActions.FinancingCreated,
            "FinancingRequest",
            financing.Id.ToString(),
            $"Created financing request {financing.Reference}.",
            cancellationToken);

        logger.LogInformation(
            "Financing request {Reference} created for parent {ParentId} status {Status}.",
            financing.Reference,
            parentId,
            financing.Status);

        return Result<FinancingRequestDto>.Success(PaymentMapping.ToFinancingDto(financing));
    }
}
