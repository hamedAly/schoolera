namespace Schoolera.Application.Payments.Constants;

public static class PaymentErrorCodes
{
    public const string Forbidden = "payments.forbidden";
    public const string NotFound = "payments.notFound";
    public const string ValidationFailed = "payments.validationFailed";
    public const string PayableNotFound = "payments.payableNotFound";
    public const string PayableNotAvailable = "payments.payableNotAvailable";
    public const string FeeNotPayable = "payments.feeNotPayable";
    public const string AmountInvalid = "payments.amountInvalid";
    public const string AlreadyPaid = "payments.alreadyPaid";
    public const string ActiveIntentExists = "payments.activeIntentExists";
    public const string ProviderNotConfigured = "payments.providerNotConfigured";
    public const string ProviderNotActive = "payments.providerNotActive";
    public const string IdempotencyConflict = "payments.idempotencyConflict";
    public const string IntentExpired = "payments.intentExpired";
    public const string InvalidTransition = "payments.invalidTransition";
    public const string ConsentRequired = "payments.consentRequired";
    public const string OwnershipRejected = "payments.ownershipRejected";
    public const string RefundNotSupported = "payments.refundNotSupported";
    public const string RefundExceedsPaid = "payments.refundExceedsPaid";
    public const string ReceiptNotFound = "payments.receiptNotFound";
    public const string WebhookRejected = "payments.webhookRejected";
    public const string ConcurrencyConflict = "payments.concurrencyConflict";
    public const string FinancingNotFound = "payments.financingNotFound";
    public const string OfferNotAvailable = "payments.offerNotAvailable";
    public const string OfferExpired = "payments.offerExpired";
    public const string ReconciliationNotFound = "payments.reconciliationNotFound";
    public const string InvalidResolution = "payments.invalidResolution";
}

public static class PaymentAuditActions
{
    public const string IntentCreated = "Payment.IntentCreated";
    public const string IntentReturned = "Payment.IntentReturned";
    public const string WebhookProcessed = "Payment.WebhookProcessed";
    public const string RefundRequested = "Payment.RefundRequested";
    public const string ReceiptCreated = "Payment.ReceiptCreated";
    public const string FinancingCreated = "Payment.FinancingCreated";
    public const string OfferSelected = "Payment.OfferSelected";
    public const string ReconciliationResolved = "Payment.ReconciliationResolved";
    public const string PayableItemUpserted = "Payment.PayableItemUpserted";
    public const string StatusRequeried = "Payment.StatusRequeried";
}
