using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface IPaymentReferenceGenerator
{
    Task<string> GeneratePaymentReferenceAsync(CancellationToken cancellationToken = default);

    Task<string> GenerateFinancingReferenceAsync(CancellationToken cancellationToken = default);

    Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken = default);
}

public sealed record ResolvedPayableAmount(
    Guid PayableItemId,
    Guid SchoolId,
    Guid SchoolBranchId,
    Guid TuitionFeeId,
    Guid? AdmissionApplicationId,
    FeeCategory FeeCategory,
    string FeeNameAr,
    string? FeeNameEn,
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal NetPayableAmount,
    string CurrencyCode,
    string? PaymentInstructionsAr,
    string? PaymentInstructionsEn,
    bool IsCurrentlyPayable,
    bool AlreadyPaid,
    bool HasActiveIntent,
    SchoolPayableItem PayableItem,
    TuitionFee TuitionFee);

public interface IPayableAmountResolver
{
    Task<ResolvedPayableAmount?> ResolveForParentAsync(
        Guid parentUserId,
        Guid payableItemId,
        Guid? admissionApplicationId,
        CancellationToken cancellationToken = default);
}

public interface IPaymentProvider
{
    string ProviderCode { get; }

    Task<PaymentCheckoutSessionResult> CreateHostedCheckoutAsync(
        PaymentIntent intent,
        PaymentIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default);

    Task<PaymentProviderStatusResult> QueryStatusAsync(
        PaymentIntent intent,
        PaymentIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default);

    Task<PaymentRefundResult> RefundAsync(
        PaymentIntent intent,
        decimal amount,
        string idempotencyKey,
        PaymentIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default);

    bool VerifyWebhookSignature(string rawBody, string? signatureHex, string? timestampUnix, string webhookSecret);
}

public interface IFinancingProvider
{
    string ProviderCode { get; }

    Task<FinancingOffersResult> RequestOffersAsync(
        FinancingRequest request,
        FinancingIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default);

    Task<FinancingProviderStatusResult> QueryStatusAsync(
        FinancingRequest request,
        FinancingIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default);

    bool VerifyWebhookSignature(string rawBody, string? signatureHex, string? timestampUnix, string webhookSecret);
}

public sealed record PaymentIntegrationSettingsContext(
    Guid IntegrationId,
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn,
    ProviderEnvironment Environment,
    string SettingsJson);

public sealed record FinancingIntegrationSettingsContext(
    Guid IntegrationId,
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn,
    ProviderEnvironment Environment,
    string SettingsJson);

public sealed record PaymentCheckoutSessionResult(
    bool Succeeded,
    string? CheckoutSessionId,
    string? RedirectUrl,
    string? SafeFailureCode,
    bool IsRetryable);

public sealed record PaymentProviderStatusResult(
    bool Succeeded,
    PaymentIntentStatus? MappedStatus,
    string? ProviderPaymentReference,
    decimal? ProviderAmount,
    string? ProviderCurrency,
    string? SafeFailureCode);

public sealed record PaymentRefundResult(
    bool Succeeded,
    string? ProviderRefundId,
    decimal Amount,
    string? SafeFailureCode,
    bool IsRetryable);

public sealed record FinancingOffersResult(
    bool Succeeded,
    string? ProviderRequestReference,
    IReadOnlyList<FinancingOfferDraft> Offers,
    string? SafeFailureCode);

public sealed record FinancingOfferDraft(
    string ProviderOfferReference,
    int TenorMonths,
    decimal PeriodicInstallment,
    decimal TotalRepayment,
    decimal? Fees,
    decimal? InterestOrProfitRate,
    decimal? AprProviderReported,
    decimal? DownPayment,
    DateTimeOffset ExpiresAtUtc,
    string? DisclosureText);

public sealed record FinancingProviderStatusResult(
    bool Succeeded,
    FinancingRequestStatus? MappedStatus,
    string? SafeFailureCode);

public interface IPaymentRepository
{
    Task<SchoolPayableItem?> GetPayableItemAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SchoolPayableItem?> GetPayableItemBySchoolAndFeeAsync(
        Guid schoolId,
        Guid tuitionFeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolPayableItem>> ListPayableItemsForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolPayableItem>> ListActivePayableItemsAsync(
        CancellationToken cancellationToken = default);

    Task AddPayableItemAsync(SchoolPayableItem item, CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetIntentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetIntentByReferenceAsync(string reference, CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetIntentByIdempotencyAsync(
        Guid parentUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetIntentByCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentIntent>> ListIntentsForParentAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentIntent>> ListIntentsForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentIntent>> ListIntentsForAdminAsync(
        PaymentIntentStatus? status,
        int take,
        CancellationToken cancellationToken = default);

    Task AddIntentAsync(PaymentIntent intent, CancellationToken cancellationToken = default);

    Task<bool> HasSucceededPaymentForPayableAsync(
        Guid payableItemId,
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveIntentForPayableAsync(
        Guid payableItemId,
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task<PaymentProviderEvent?> GetProviderEventAsync(
        string providerEventId,
        CancellationToken cancellationToken = default);

    Task AddProviderEventAsync(PaymentProviderEvent evt, CancellationToken cancellationToken = default);

    Task AddReceiptAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default);

    Task<PaymentReceipt?> GetReceiptByIntentIdAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken = default);

    Task<PaymentReceipt?> GetReceiptByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddReconciliationAsync(
        PaymentReconciliationRecord record,
        CancellationToken cancellationToken = default);

    Task<PaymentReconciliationRecord?> GetReconciliationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentReconciliationRecord>> ListReconciliationAsync(
        PaymentReconciliationStatus? status,
        CancellationToken cancellationToken = default);

    Task<FinancingRequest?> GetFinancingByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FinancingRequest?> GetFinancingByReferenceAsync(
        string reference,
        CancellationToken cancellationToken = default);

    Task<FinancingRequest?> GetFinancingByIdempotencyAsync(
        Guid parentUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancingRequest>> ListFinancingForParentAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task AddFinancingAsync(FinancingRequest request, CancellationToken cancellationToken = default);

    Task<decimal> SumSuccessfulRefundsAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken = default);
}
