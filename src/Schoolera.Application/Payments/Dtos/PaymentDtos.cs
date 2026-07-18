using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Dtos;

public sealed record PublicPaymentProviderDto(
    Guid IntegrationId,
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn,
    string IntegrationType,
    string Environment,
    IReadOnlyList<string> SupportedCurrencies,
    IReadOnlyList<string> SupportedMethods,
    IReadOnlyList<int>? SupportedTenorsMonths,
    decimal MinimumAmount,
    decimal? MaximumAmount,
    string? TermsUrl,
    string? PrivacyUrl,
    bool IsDefault,
    bool IsSandbox);

public sealed record ParentPayableItemDto(
    Guid PayableItemId,
    Guid SchoolId,
    Guid SchoolBranchId,
    Guid TuitionFeeId,
    string SchoolNameAr,
    string? SchoolNameEn,
    string FeeNameAr,
    string? FeeNameEn,
    string FeeCategory,
    decimal Amount,
    string CurrencyCode,
    string? PaymentInstructionsAr,
    string? PaymentInstructionsEn,
    bool IsCurrentlyPayable,
    bool AlreadyPaid,
    bool HasActiveIntent);

public sealed record PaymentSummaryDto(
    Guid PayableItemId,
    Guid SchoolId,
    Guid SchoolBranchId,
    Guid? AdmissionApplicationId,
    string FeeCategory,
    string FeeNameAr,
    string? FeeNameEn,
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal NetPayableAmount,
    string CurrencyCode,
    bool IsPayable,
    bool AlreadyPaid,
    bool HasActiveIntent,
    string? PaymentInstructionsAr,
    string? PaymentInstructionsEn,
    IReadOnlyList<PublicPaymentProviderDto> AvailableProviders);

public sealed record CreatePaymentIntentRequest(
    Guid PayableItemId,
    Guid? AdmissionApplicationId,
    Guid? IntegrationConfigurationId,
    PaymentMethodKind PaymentMethod,
    string IdempotencyKey,
    bool TermsAccepted,
    bool PrivacyAccepted);

public sealed record PaymentIntentDto(
    Guid Id,
    string Reference,
    Guid PayableItemId,
    Guid SchoolId,
    decimal Amount,
    string CurrencyCode,
    string ProviderCode,
    string ProviderDisplayNameAr,
    string? ProviderDisplayNameEn,
    string Environment,
    string PaymentMethod,
    string Status,
    string? RedirectUrl,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? SucceededAtUtc,
    string? SafeFailureCode,
    bool IsSandbox,
    DateTimeOffset CreatedAtUtc);

public sealed record PaymentReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid PaymentIntentId,
    string PaymentReference,
    Guid SchoolId,
    decimal Amount,
    string CurrencyCode,
    string ProviderPublicName,
    string? ProviderPaymentReference,
    string Environment,
    bool IsSandbox,
    bool IsTaxInvoice,
    DateTimeOffset PaidAtUtc,
    string RefundStatus);

public sealed record CreateFinancingRequestBody(
    Guid PayableItemId,
    Guid? AdmissionApplicationId,
    Guid? IntegrationConfigurationId,
    string IdempotencyKey,
    bool TermsAccepted,
    bool PrivacyAccepted);

public sealed record SelectFinancingOfferRequest(
    Guid OfferId,
    bool ConsentAccepted);

public sealed record FinancingOfferDto(
    Guid Id,
    string ProviderOfferReference,
    int TenorMonths,
    decimal PeriodicInstallment,
    decimal TotalRepayment,
    decimal? Fees,
    decimal? InterestOrProfitRate,
    decimal? AprProviderReported,
    bool AprIsProviderReported,
    decimal? DownPayment,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    string? DisclosureText);

public sealed record FinancingRequestDto(
    Guid Id,
    string Reference,
    Guid PayableItemId,
    Guid SchoolId,
    decimal Amount,
    string CurrencyCode,
    string ProviderCode,
    string Environment,
    string Status,
    Guid? SelectedOfferId,
    bool IsSandbox,
    IReadOnlyList<FinancingOfferDto> Offers,
    DateTimeOffset CreatedAtUtc);

public sealed record SchoolPayableItemAdminDto(
    Guid Id,
    Guid TuitionFeeId,
    Guid SchoolBranchId,
    string FeeNameAr,
    string? FeeNameEn,
    decimal FeeAmount,
    string CurrencyCode,
    bool FeeIsPublished,
    DateTimeOffset? PayableFromUtc,
    DateTimeOffset? PayableToUtc,
    string? PaymentInstructionsAr,
    string? PaymentInstructionsEn,
    bool IsActive,
    bool IsCurrentlyPayable);

public sealed record UpsertSchoolPayableItemRequest(
    Guid TuitionFeeId,
    DateTimeOffset? PayableFromUtc,
    DateTimeOffset? PayableToUtc,
    string? PaymentInstructionsAr,
    string? PaymentInstructionsEn,
    bool IsActive,
    string? RowVersion);

public sealed record SchoolSettlementPaymentDto(
    Guid PaymentIntentId,
    string Reference,
    Guid PayableItemId,
    Guid? AdmissionApplicationId,
    decimal Amount,
    string CurrencyCode,
    string Status,
    string ProviderPublicName,
    string Environment,
    bool IsSandbox,
    DateTimeOffset? VerifiedAtUtc,
    string RefundStatus,
    bool OnReconciliationHold);

public sealed record AdminPaymentMonitoringDto(
    Guid Id,
    string Reference,
    Guid SchoolId,
    Guid ParentUserId,
    decimal Amount,
    string CurrencyCode,
    string Status,
    string ProviderCode,
    string Environment,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SucceededAtUtc,
    string? SafeFailureCode);

public sealed record ReconciliationRecordDto(
    Guid Id,
    Guid? PaymentIntentId,
    Guid? FinancingRequestId,
    string ProviderCode,
    string Environment,
    string MismatchType,
    string Status,
    string InternalStatus,
    string? ProviderStatus,
    decimal? ExpectedAmount,
    decimal? ProviderAmount,
    string CurrencyCode,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    string? SafeResolutionCode,
    string? InternalNote);

public sealed record ResolveReconciliationRequest(
    string ResolutionCode,
    string? InternalNote);

public sealed record RequestPaymentRefundRequest(
    decimal? Amount,
    string ReasonCode,
    string? SafeNote,
    string IdempotencyKey);

public sealed record SandboxWebhookAckDto(bool Accepted, string? SafeCode);

public sealed record PaymentReturnRequest(string Reference);

public sealed record InvestigateReconciliationBody(string? Note);
