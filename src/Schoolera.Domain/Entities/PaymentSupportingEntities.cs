using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class PaymentTransaction
{
    private PaymentTransaction()
    {
    }

    public PaymentTransaction(
        Guid paymentIntentId,
        PaymentTransactionType type,
        decimal amount,
        string currencyCode,
        string? providerTransactionId,
        bool succeeded)
    {
        Id = Guid.NewGuid();
        PaymentIntentId = paymentIntentId;
        Type = type;
        Amount = amount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ProviderTransactionId = string.IsNullOrWhiteSpace(providerTransactionId)
            ? null
            : providerTransactionId.Trim();
        Succeeded = succeeded;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PaymentIntentId { get; private set; }

    public PaymentTransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; } = "EGP";

    public string? ProviderTransactionId { get; private set; }

    public bool Succeeded { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class PaymentProviderEvent
{
    private PaymentProviderEvent()
    {
    }

    public PaymentProviderEvent(
        Guid? paymentIntentId,
        Guid? financingRequestId,
        string providerEventId,
        string eventType,
        bool processed,
        string? safeSummary)
    {
        Id = Guid.NewGuid();
        PaymentIntentId = paymentIntentId;
        FinancingRequestId = financingRequestId;
        ProviderEventId = providerEventId.Trim();
        EventType = eventType.Trim();
        Processed = processed;
        SafeSummary = string.IsNullOrWhiteSpace(safeSummary) ? null : safeSummary.Trim();
        ReceivedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid? PaymentIntentId { get; private set; }

    public Guid? FinancingRequestId { get; private set; }

    public string ProviderEventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public bool Processed { get; private set; }

    public string? SafeSummary { get; private set; }

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public void MarkProcessed() => Processed = true;
}

public sealed class PaymentReceipt
{
    private PaymentReceipt()
    {
    }

    public PaymentReceipt(
        Guid paymentIntentId,
        string receiptNumber,
        Guid parentUserId,
        Guid schoolId,
        decimal amount,
        string currencyCode,
        ProviderEnvironment environment,
        string providerPublicName,
        string? providerPaymentReference,
        DateTimeOffset paidAtUtc)
    {
        Id = Guid.NewGuid();
        PaymentIntentId = paymentIntentId;
        ReceiptNumber = receiptNumber.Trim();
        ParentUserId = parentUserId;
        SchoolId = schoolId;
        Amount = amount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Environment = environment;
        ProviderPublicName = providerPublicName.Trim();
        ProviderPaymentReference = providerPaymentReference;
        PaidAtUtc = paidAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsTaxInvoice = false;
    }

    public Guid Id { get; private set; }

    public Guid PaymentIntentId { get; private set; }

    public string ReceiptNumber { get; private set; } = string.Empty;

    public Guid ParentUserId { get; private set; }

    public Guid SchoolId { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; } = "EGP";

    public ProviderEnvironment Environment { get; private set; }

    public string ProviderPublicName { get; private set; } = string.Empty;

    public string? ProviderPaymentReference { get; private set; }

    public DateTimeOffset PaidAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Platform receipt is not a tax invoice unless legally established.</summary>
    public bool IsTaxInvoice { get; private set; }
}

public sealed class PaymentReconciliationRecord
{
    private PaymentReconciliationRecord()
    {
    }

    public PaymentReconciliationRecord(
        Guid? paymentIntentId,
        Guid? financingRequestId,
        string providerCode,
        ProviderEnvironment environment,
        PaymentReconciliationMismatchType mismatchType,
        string internalStatus,
        string? providerStatus,
        decimal? expectedAmount,
        decimal? providerAmount,
        string currencyCode)
    {
        Id = Guid.NewGuid();
        PaymentIntentId = paymentIntentId;
        FinancingRequestId = financingRequestId;
        ProviderCode = providerCode.Trim();
        Environment = environment;
        MismatchType = mismatchType;
        Status = PaymentReconciliationStatus.Open;
        InternalStatus = internalStatus.Trim();
        ProviderStatus = providerStatus;
        ExpectedAmount = expectedAmount;
        ProviderAmount = providerAmount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        DetectedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid? PaymentIntentId { get; private set; }

    public Guid? FinancingRequestId { get; private set; }

    public string ProviderCode { get; private set; } = string.Empty;

    public ProviderEnvironment Environment { get; private set; }

    public PaymentReconciliationMismatchType MismatchType { get; private set; }

    public PaymentReconciliationStatus Status { get; private set; }

    public string InternalStatus { get; private set; } = string.Empty;

    public string? ProviderStatus { get; private set; }

    public decimal? ExpectedAmount { get; private set; }

    public decimal? ProviderAmount { get; private set; }

    public string CurrencyCode { get; private set; } = "EGP";

    public DateTimeOffset DetectedAtUtc { get; private set; }

    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public string? SafeResolutionCode { get; private set; }

    public string? InternalNote { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public void MarkInvestigating(string? note)
    {
        Status = PaymentReconciliationStatus.Investigating;
        InternalNote = note;
    }

    public void Resolve(Guid actorUserId, string resolutionCode, string? note)
    {
        Status = PaymentReconciliationStatus.Resolved;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
        ResolvedByUserId = actorUserId;
        SafeResolutionCode = resolutionCode.Trim();
        InternalNote = note;
    }
}

public sealed class PaymentReferenceSequence
{
    private PaymentReferenceSequence()
    {
    }

    public PaymentReferenceSequence(string dayKey)
    {
        DayKey = dayKey;
        LastValue = 0;
    }

    public string DayKey { get; private set; } = string.Empty;

    public long LastValue { get; private set; }

    public long Next()
    {
        LastValue += 1;
        return LastValue;
    }
}
