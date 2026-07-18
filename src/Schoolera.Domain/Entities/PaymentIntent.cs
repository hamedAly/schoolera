using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class PaymentIntent
{
    private readonly List<PaymentTransaction> _transactions = [];
    private readonly List<PaymentProviderEvent> _events = [];

    private PaymentIntent()
    {
    }

    public PaymentIntent(
        string reference,
        Guid parentUserId,
        Guid schoolId,
        Guid schoolBranchId,
        Guid payableItemId,
        Guid? admissionApplicationId,
        decimal amount,
        string currencyCode,
        Guid integrationConfigurationId,
        string providerCode,
        ProviderEnvironment environment,
        PaymentMethodKind paymentMethod,
        string idempotencyKey,
        DateTimeOffset expiresAtUtc)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Id = Guid.NewGuid();
        Reference = reference.Trim();
        ParentUserId = parentUserId;
        SchoolId = schoolId;
        SchoolBranchId = schoolBranchId;
        PayableItemId = payableItemId;
        AdmissionApplicationId = admissionApplicationId;
        Amount = amount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        IntegrationConfigurationId = integrationConfigurationId;
        ProviderCode = providerCode.Trim();
        Environment = environment;
        PaymentMethod = paymentMethod;
        Status = PaymentIntentStatus.Created;
        IdempotencyKey = idempotencyKey.Trim();
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public Guid ParentUserId { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid SchoolBranchId { get; private set; }

    public Guid PayableItemId { get; private set; }

    public Guid? AdmissionApplicationId { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; } = "EGP";

    public Guid IntegrationConfigurationId { get; private set; }

    public string ProviderCode { get; private set; } = string.Empty;

    public ProviderEnvironment Environment { get; private set; }

    public PaymentMethodKind PaymentMethod { get; private set; }

    public PaymentIntentStatus Status { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string? ProviderCheckoutSessionId { get; private set; }

    public string? ProviderPaymentReference { get; private set; }

    public string? RedirectUrl { get; private set; }

    public DateTimeOffset? ReturnedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? SucceededAtUtc { get; private set; }

    public string? SafeFailureCode { get; private set; }

    public Guid? ConsentDocumentVersionId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<PaymentTransaction> Transactions => _transactions;

    public IReadOnlyCollection<PaymentProviderEvent> Events => _events;

    public void MarkPendingProvider(string checkoutSessionId, string? redirectUrl)
    {
        ProviderCheckoutSessionId = checkoutSessionId;
        RedirectUrl = redirectUrl;
        Status = PaymentIntentStatus.RequiresParentAction;
        Touch();
    }

    public void MarkReturned()
    {
        ReturnedAtUtc ??= DateTimeOffset.UtcNow;
        if (Status is PaymentIntentStatus.RequiresParentAction or PaymentIntentStatus.Created)
        {
            Status = PaymentIntentStatus.Processing;
        }

        Touch();
    }

    public bool TryApplyVerifiedStatus(PaymentIntentStatus next, DateTimeOffset utcNow, string? failureCode = null)
    {
        if (!PaymentIntentTransitionPolicy.CanTransition(Status, next))
        {
            return false;
        }

        Status = next;
        SafeFailureCode = failureCode;
        if (next == PaymentIntentStatus.Succeeded)
        {
            SucceededAtUtc ??= utcNow;
        }

        Touch();
        return true;
    }

    public void AttachProviderPaymentReference(string reference)
    {
        ProviderPaymentReference = reference.Trim();
        Touch();
    }

    public PaymentTransaction AddTransaction(
        PaymentTransactionType type,
        decimal amount,
        string currencyCode,
        string? providerTransactionId,
        bool succeeded)
    {
        var tx = new PaymentTransaction(Id, type, amount, currencyCode, providerTransactionId, succeeded);
        _transactions.Add(tx);
        Touch();
        return tx;
    }

    public void AddEvent(string providerEventId, string eventType, bool processed, string? safeSummary)
    {
        _events.Add(new PaymentProviderEvent(Id, null, providerEventId, eventType, processed, safeSummary));
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

/// <summary>Centralized payment intent status transitions.</summary>
public static class PaymentIntentTransitionPolicy
{
    public static bool CanTransition(PaymentIntentStatus from, PaymentIntentStatus to)
    {
        if (from == to)
        {
            return true; // idempotent no-op treated as success by callers
        }

        return (from, to) switch
        {
            (PaymentIntentStatus.Created, PaymentIntentStatus.PendingProvider) => true,
            (PaymentIntentStatus.Created, PaymentIntentStatus.RequiresParentAction) => true,
            (PaymentIntentStatus.Created, PaymentIntentStatus.Cancelled) => true,
            (PaymentIntentStatus.Created, PaymentIntentStatus.Expired) => true,
            (PaymentIntentStatus.PendingProvider, PaymentIntentStatus.RequiresParentAction) => true,
            (PaymentIntentStatus.PendingProvider, PaymentIntentStatus.Processing) => true,
            (PaymentIntentStatus.PendingProvider, PaymentIntentStatus.Failed) => true,
            (PaymentIntentStatus.PendingProvider, PaymentIntentStatus.Cancelled) => true,
            (PaymentIntentStatus.PendingProvider, PaymentIntentStatus.Expired) => true,
            (PaymentIntentStatus.RequiresParentAction, PaymentIntentStatus.Processing) => true,
            (PaymentIntentStatus.RequiresParentAction, PaymentIntentStatus.Succeeded) => true,
            (PaymentIntentStatus.RequiresParentAction, PaymentIntentStatus.Failed) => true,
            (PaymentIntentStatus.RequiresParentAction, PaymentIntentStatus.Cancelled) => true,
            (PaymentIntentStatus.RequiresParentAction, PaymentIntentStatus.Expired) => true,
            (PaymentIntentStatus.Processing, PaymentIntentStatus.Succeeded) => true,
            (PaymentIntentStatus.Processing, PaymentIntentStatus.Failed) => true,
            (PaymentIntentStatus.Processing, PaymentIntentStatus.Cancelled) => true,
            (PaymentIntentStatus.Succeeded, PaymentIntentStatus.PartiallyRefunded) => true,
            (PaymentIntentStatus.Succeeded, PaymentIntentStatus.Refunded) => true,
            (PaymentIntentStatus.PartiallyRefunded, PaymentIntentStatus.Refunded) => true,
            (PaymentIntentStatus.PartiallyRefunded, PaymentIntentStatus.PartiallyRefunded) => true,
            _ => false,
        };
    }

    public static bool IsTerminalSuccess(PaymentIntentStatus status) =>
        status is PaymentIntentStatus.Succeeded or PaymentIntentStatus.PartiallyRefunded or PaymentIntentStatus.Refunded;
}
