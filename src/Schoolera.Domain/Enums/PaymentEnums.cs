namespace Schoolera.Domain.Enums;

public enum ProviderEnvironment
{
    Sandbox = 1,
    Production = 2,
}

public enum PaymentMethodKind
{
    HostedCardCheckout = 1,
    BankTransferRedirect = 2,
    MobileWallet = 3,
    CashNetwork = 4,
    ProviderHostedCheckout = 5,
}

public enum PaymentIntentStatus
{
    Created = 1,
    PendingProvider = 2,
    RequiresParentAction = 3,
    Processing = 4,
    Succeeded = 5,
    Failed = 6,
    Cancelled = 7,
    Expired = 8,
    PartiallyRefunded = 9,
    Refunded = 10,
}

public enum PaymentTransactionType
{
    Authorization = 1,
    Charge = 2,
    Capture = 3,
    Refund = 4,
    Reversal = 5,
    ProviderAdjustment = 6,
}

public enum FinancingRequestStatus
{
    Created = 1,
    OffersRequested = 2,
    OffersAvailable = 3,
    OfferSelected = 4,
    ProviderReview = 5,
    Approved = 6,
    Declined = 7,
    Expired = 8,
    Cancelled = 9,
    FundingPending = 10,
    Funded = 11,
}

public enum FinancingOfferStatus
{
    Available = 1,
    Selected = 2,
    Expired = 3,
    Withdrawn = 4,
}

public enum PaymentReconciliationMismatchType
{
    StatusMismatch = 1,
    AmountMismatch = 2,
    CurrencyMismatch = 3,
    MissingProviderRecord = 4,
    DuplicateProviderEvent = 5,
    Other = 6,
}

public enum PaymentReconciliationStatus
{
    Open = 1,
    Investigating = 2,
    Resolved = 3,
}
