using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Common;

public sealed class PayableAmountResolver(
    IPaymentRepository paymentRepository,
    IAdmissionApplicationRepository admissionRepository) : IPayableAmountResolver
{
    public async Task<ResolvedPayableAmount?> ResolveForParentAsync(
        Guid parentUserId,
        Guid payableItemId,
        Guid? admissionApplicationId,
        CancellationToken cancellationToken = default)
    {
        var payable = await paymentRepository.GetPayableItemAsync(payableItemId, cancellationToken);
        if (payable is null)
        {
            return null;
        }

        var fee = payable.TuitionFee;
        if (fee is null || !fee.IsActive || !fee.IsPublished || fee.Amount <= 0m)
        {
            return null;
        }

        if (admissionApplicationId is { } appId)
        {
            var admission = await admissionRepository.GetOwnedAsync(parentUserId, appId, cancellationToken);
            if (admission is null || admission.SchoolId != payable.SchoolId)
            {
                return null;
            }
        }

        var alreadyPaid = await paymentRepository.HasSucceededPaymentForPayableAsync(
            payableItemId, parentUserId, cancellationToken);
        var hasActive = await paymentRepository.HasActiveIntentForPayableAsync(
            payableItemId, parentUserId, cancellationToken);

        var feeNameAr = fee.NameAr ?? fee.Category.ToString();
        return new ResolvedPayableAmount(
            payable.Id,
            payable.SchoolId,
            payable.SchoolBranchId,
            fee.Id,
            admissionApplicationId,
            fee.Category,
            feeNameAr,
            fee.NameEn,
            fee.Amount,
            DiscountAmount: 0m,
            NetPayableAmount: fee.Amount,
            fee.CurrencyCode,
            payable.PaymentInstructionsAr,
            payable.PaymentInstructionsEn,
            payable.IsCurrentlyPayable(DateTimeOffset.UtcNow),
            alreadyPaid,
            hasActive,
            payable,
            fee);
    }
}

public static class PaymentNotificationSupport
{
    public static async Task EnqueueAsync(
        INotificationOutboxPublisher publisher,
        IParentAccountService parentAccountService,
        Guid parentUserId,
        Guid entityId,
        NotificationEventType eventType,
        string actionKey,
        string reference,
        string actionPath,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        var account = await parentAccountService.GetAsync(parentUserId, cancellationToken);
        var culture = NormalizeCulture(account?.PreferredLanguage);

        await publisher.EnqueueAsync(
            new NotificationEnqueueRequest(
                parentUserId,
                eventType,
                culture,
                DeduplicationKeyBase: $"payment:{entityId}:{actionKey}:{(int)eventType}",
                Variables: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["reference"] = reference,
                    ["eventType"] = eventType.ToString(),
                },
                ActionPath: actionPath,
                RelatedSchoolId: schoolId,
                RelatedEntityId: entityId),
            cancellationToken);
    }

    private static string NormalizeCulture(string? preferredLanguage)
    {
        if (string.IsNullOrWhiteSpace(preferredLanguage))
        {
            return "ar";
        }

        var value = preferredLanguage.Trim().ToLowerInvariant();
        return value.StartsWith("en", StringComparison.Ordinal) ? "en" : "ar";
    }
}

public static class PaymentMapping
{
    public static Dtos.PaymentIntentDto ToIntentDto(
        Domain.Entities.PaymentIntent intent,
        string displayNameAr,
        string? displayNameEn) =>
        new(
            intent.Id,
            intent.Reference,
            intent.PayableItemId,
            intent.SchoolId,
            intent.Amount,
            intent.CurrencyCode,
            intent.ProviderCode,
            displayNameAr,
            displayNameEn,
            intent.Environment.ToString(),
            intent.PaymentMethod.ToString(),
            intent.Status.ToString(),
            intent.RedirectUrl,
            intent.ExpiresAtUtc,
            intent.SucceededAtUtc,
            intent.SafeFailureCode,
            intent.Environment == ProviderEnvironment.Sandbox,
            intent.CreatedAtUtc);

    public static Dtos.FinancingOfferDto ToOfferDto(Domain.Entities.FinancingOffer offer) =>
        new(
            offer.Id,
            offer.ProviderOfferReference,
            offer.TenorMonths,
            offer.PeriodicInstallment,
            offer.TotalRepayment,
            offer.Fees,
            offer.InterestOrProfitRate,
            offer.AprProviderReported,
            AprIsProviderReported: offer.AprProviderReported is not null,
            offer.DownPayment,
            offer.ExpiresAtUtc,
            offer.Status.ToString(),
            offer.DisclosureText);

    public static Dtos.FinancingRequestDto ToFinancingDto(Domain.Entities.FinancingRequest request) =>
        new(
            request.Id,
            request.Reference,
            request.PayableItemId,
            request.SchoolId,
            request.Amount,
            request.CurrencyCode,
            request.ProviderCode,
            request.Environment.ToString(),
            request.Status.ToString(),
            request.SelectedOfferId,
            request.Environment == ProviderEnvironment.Sandbox,
            request.Offers.Select(ToOfferDto).ToList(),
            request.CreatedAtUtc);

    public static Dtos.PaymentReceiptDto ToReceiptDto(
        Domain.Entities.PaymentReceipt receipt,
        Domain.Entities.PaymentIntent intent) =>
        new(
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.PaymentIntentId,
            intent.Reference,
            receipt.SchoolId,
            receipt.Amount,
            receipt.CurrencyCode,
            receipt.ProviderPublicName,
            receipt.ProviderPaymentReference,
            receipt.Environment.ToString(),
            receipt.Environment == ProviderEnvironment.Sandbox,
            receipt.IsTaxInvoice,
            receipt.PaidAtUtc,
            RefundStatus(intent.Status));

    public static Dtos.SchoolPayableItemAdminDto ToSchoolPayableAdminDto(
        Domain.Entities.SchoolPayableItem item) =>
        new(
            item.Id,
            item.TuitionFeeId,
            item.SchoolBranchId,
            item.TuitionFee.NameAr ?? item.TuitionFee.Category.ToString(),
            item.TuitionFee.NameEn,
            item.TuitionFee.Amount,
            item.TuitionFee.CurrencyCode,
            item.TuitionFee.IsPublished,
            item.PayableFromUtc,
            item.PayableToUtc,
            item.PaymentInstructionsAr,
            item.PaymentInstructionsEn,
            item.IsActive,
            item.IsCurrentlyPayable(DateTimeOffset.UtcNow));

    public static Dtos.SchoolSettlementPaymentDto ToSettlementDto(
        Domain.Entities.PaymentIntent intent,
        string providerPublicName,
        bool onReconciliationHold) =>
        new(
            intent.Id,
            intent.Reference,
            intent.PayableItemId,
            intent.AdmissionApplicationId,
            intent.Amount,
            intent.CurrencyCode,
            intent.Status.ToString(),
            providerPublicName,
            intent.Environment.ToString(),
            intent.Environment == ProviderEnvironment.Sandbox,
            intent.SucceededAtUtc,
            RefundStatus(intent.Status),
            onReconciliationHold);

    public static Dtos.AdminPaymentMonitoringDto ToMonitoringDto(Domain.Entities.PaymentIntent intent) =>
        new(
            intent.Id,
            intent.Reference,
            intent.SchoolId,
            intent.ParentUserId,
            intent.Amount,
            intent.CurrencyCode,
            intent.Status.ToString(),
            intent.ProviderCode,
            intent.Environment.ToString(),
            intent.CreatedAtUtc,
            intent.SucceededAtUtc,
            intent.SafeFailureCode);

    public static Dtos.ReconciliationRecordDto ToReconciliationDto(
        Domain.Entities.PaymentReconciliationRecord record) =>
        new(
            record.Id,
            record.PaymentIntentId,
            record.FinancingRequestId,
            record.ProviderCode,
            record.Environment.ToString(),
            record.MismatchType.ToString(),
            record.Status.ToString(),
            record.InternalStatus,
            record.ProviderStatus,
            record.ExpectedAmount,
            record.ProviderAmount,
            record.CurrencyCode,
            record.DetectedAtUtc,
            record.ResolvedAtUtc,
            record.SafeResolutionCode,
            record.InternalNote);

    public static Dtos.PublicPaymentProviderDto ToPublicProviderDto(
        Domain.Entities.PlatformIntegrationConfiguration entity,
        PaymentIntegrationSettings settings,
        bool isDefault) =>
        new(
            entity.Id,
            entity.ProviderCode,
            entity.DisplayNameAr,
            entity.DisplayNameEn,
            entity.IntegrationType.ToString(),
            settings.Environment,
            settings.SupportedCurrencies,
            settings.SupportedPaymentMethods,
            SupportedTenorsMonths: null,
            settings.MinimumAmount,
            settings.MaximumAmount,
            settings.TermsUrl,
            settings.PrivacyUrl,
            isDefault,
            ParseEnvironment(settings.Environment) == ProviderEnvironment.Sandbox);

    public static Dtos.PublicPaymentProviderDto ToPublicFinancingProviderDto(
        Domain.Entities.PlatformIntegrationConfiguration entity,
        FinancingIntegrationSettings settings,
        bool isDefault) =>
        new(
            entity.Id,
            entity.ProviderCode,
            entity.DisplayNameAr,
            entity.DisplayNameEn,
            entity.IntegrationType.ToString(),
            settings.Environment,
            settings.SupportedCurrencies,
            settings.SupportedFinancingMethods,
            settings.SupportedTenorsMonths,
            settings.MinimumAmount,
            settings.MaximumAmount,
            settings.TermsUrl,
            settings.PrivacyUrl,
            isDefault,
            ParseEnvironment(settings.Environment) == ProviderEnvironment.Sandbox);

    public static string RefundStatus(PaymentIntentStatus status) =>
        status switch
        {
            PaymentIntentStatus.Refunded => "Full",
            PaymentIntentStatus.PartiallyRefunded => "Partial",
            _ => "None",
        };

    public static bool TryParseRowVersion(string? value, out byte[]? rowVersion)
    {
        rowVersion = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool HasRowVersionMismatch(byte[]? requested, byte[] current) =>
        requested is { Length: > 0 } &&
        current.Length > 0 &&
        !requested.AsSpan().SequenceEqual(current);

    public static ProviderEnvironment ParseEnvironment(string? value) =>
        string.Equals(value, "Production", StringComparison.OrdinalIgnoreCase)
            ? ProviderEnvironment.Production
            : ProviderEnvironment.Sandbox;
}
