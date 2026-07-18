using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class FinancingRequest
{
    private readonly List<FinancingOffer> _offers = [];
    private readonly List<FinancingDecisionEvent> _decisions = [];

    private FinancingRequest()
    {
    }

    public FinancingRequest(
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
        string idempotencyKey,
        Guid? consentDocumentVersionId)
    {
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
        Status = FinancingRequestStatus.Created;
        IdempotencyKey = idempotencyKey.Trim();
        ConsentDocumentVersionId = consentDocumentVersionId;
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

    public FinancingRequestStatus Status { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public Guid? ConsentDocumentVersionId { get; private set; }

    public Guid? SelectedOfferId { get; private set; }

    public string? ProviderRequestReference { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<FinancingOffer> Offers => _offers;

    public IReadOnlyCollection<FinancingDecisionEvent> Decisions => _decisions;

    public void MarkOffersAvailable(string providerRequestReference)
    {
        ProviderRequestReference = providerRequestReference;
        Status = FinancingRequestStatus.OffersAvailable;
        Touch();
    }

    public FinancingOffer AddOffer(
        string providerOfferReference,
        int tenorMonths,
        decimal periodicInstallment,
        decimal totalRepayment,
        decimal? fees,
        decimal? interestOrProfitRate,
        decimal? aprProviderReported,
        decimal? downPayment,
        DateTimeOffset expiresAtUtc,
        string? disclosureText)
    {
        var offer = new FinancingOffer(
            Id,
            providerOfferReference,
            tenorMonths,
            periodicInstallment,
            totalRepayment,
            fees,
            interestOrProfitRate,
            aprProviderReported,
            downPayment,
            expiresAtUtc,
            disclosureText);
        _offers.Add(offer);
        Touch();
        return offer;
    }

    public bool TrySelectOffer(Guid offerId, DateTimeOffset utcNow)
    {
        var offer = _offers.FirstOrDefault(o => o.Id == offerId);
        if (offer is null || offer.Status != FinancingOfferStatus.Available || offer.ExpiresAtUtc <= utcNow)
        {
            return false;
        }

        if (Status is not (FinancingRequestStatus.OffersAvailable or FinancingRequestStatus.OfferSelected))
        {
            return false;
        }

        foreach (var item in _offers)
        {
            if (item.Id == offerId)
            {
                item.MarkSelected();
            }
        }

        SelectedOfferId = offerId;
        Status = FinancingRequestStatus.OfferSelected;
        Touch();
        return true;
    }

    public bool TryApplyStatus(FinancingRequestStatus next)
    {
        if (!FinancingRequestTransitionPolicy.CanTransition(Status, next))
        {
            return false;
        }

        Status = next;
        Touch();
        return true;
    }

    public void AddDecision(string decisionType, string? safeSummary)
    {
        _decisions.Add(new FinancingDecisionEvent(Id, decisionType, safeSummary));
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

public sealed class FinancingOffer
{
    private FinancingOffer()
    {
    }

    public FinancingOffer(
        Guid financingRequestId,
        string providerOfferReference,
        int tenorMonths,
        decimal periodicInstallment,
        decimal totalRepayment,
        decimal? fees,
        decimal? interestOrProfitRate,
        decimal? aprProviderReported,
        decimal? downPayment,
        DateTimeOffset expiresAtUtc,
        string? disclosureText)
    {
        Id = Guid.NewGuid();
        FinancingRequestId = financingRequestId;
        ProviderOfferReference = providerOfferReference.Trim();
        TenorMonths = tenorMonths;
        PeriodicInstallment = periodicInstallment;
        TotalRepayment = totalRepayment;
        Fees = fees;
        InterestOrProfitRate = interestOrProfitRate;
        AprProviderReported = aprProviderReported;
        DownPayment = downPayment;
        ExpiresAtUtc = expiresAtUtc;
        DisclosureText = disclosureText;
        Status = FinancingOfferStatus.Available;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid FinancingRequestId { get; private set; }

    public string ProviderOfferReference { get; private set; } = string.Empty;

    public int TenorMonths { get; private set; }

    public decimal PeriodicInstallment { get; private set; }

    public decimal TotalRepayment { get; private set; }

    public decimal? Fees { get; private set; }

    public decimal? InterestOrProfitRate { get; private set; }

    /// <summary>Only when the provider returns APR — never invented by Schoolera.</summary>
    public decimal? AprProviderReported { get; private set; }

    public decimal? DownPayment { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public string? DisclosureText { get; private set; }

    public FinancingOfferStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void MarkSelected() => Status = FinancingOfferStatus.Selected;

    public void MarkExpired() => Status = FinancingOfferStatus.Expired;
}

public sealed class FinancingDecisionEvent
{
    private FinancingDecisionEvent()
    {
    }

    public FinancingDecisionEvent(Guid financingRequestId, string decisionType, string? safeSummary)
    {
        Id = Guid.NewGuid();
        FinancingRequestId = financingRequestId;
        DecisionType = decisionType.Trim();
        SafeSummary = safeSummary;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid FinancingRequestId { get; private set; }

    public string DecisionType { get; private set; } = string.Empty;

    public string? SafeSummary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public static class FinancingRequestTransitionPolicy
{
    public static bool CanTransition(FinancingRequestStatus from, FinancingRequestStatus to)
    {
        if (from == to)
        {
            return true;
        }

        return (from, to) switch
        {
            (FinancingRequestStatus.Created, FinancingRequestStatus.OffersRequested) => true,
            (FinancingRequestStatus.Created, FinancingRequestStatus.OffersAvailable) => true,
            (FinancingRequestStatus.Created, FinancingRequestStatus.Declined) => true,
            (FinancingRequestStatus.Created, FinancingRequestStatus.Cancelled) => true,
            (FinancingRequestStatus.OffersRequested, FinancingRequestStatus.OffersAvailable) => true,
            (FinancingRequestStatus.OffersRequested, FinancingRequestStatus.Declined) => true,
            (FinancingRequestStatus.OffersRequested, FinancingRequestStatus.Expired) => true,
            (FinancingRequestStatus.OffersAvailable, FinancingRequestStatus.OfferSelected) => true,
            (FinancingRequestStatus.OffersAvailable, FinancingRequestStatus.Expired) => true,
            (FinancingRequestStatus.OffersAvailable, FinancingRequestStatus.Cancelled) => true,
            (FinancingRequestStatus.OfferSelected, FinancingRequestStatus.ProviderReview) => true,
            (FinancingRequestStatus.OfferSelected, FinancingRequestStatus.Approved) => true,
            (FinancingRequestStatus.OfferSelected, FinancingRequestStatus.Declined) => true,
            (FinancingRequestStatus.ProviderReview, FinancingRequestStatus.Approved) => true,
            (FinancingRequestStatus.ProviderReview, FinancingRequestStatus.Declined) => true,
            (FinancingRequestStatus.Approved, FinancingRequestStatus.FundingPending) => true,
            (FinancingRequestStatus.Approved, FinancingRequestStatus.Funded) => true,
            (FinancingRequestStatus.FundingPending, FinancingRequestStatus.Funded) => true,
            _ => false,
        };
    }
}
