using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class CourierProviderProfile
{
    private CourierProviderProfile()
    {
    }

    public CourierProviderProfile(
        Guid integrationId,
        string descriptionAr,
        string descriptionEn,
        string? logoReference,
        string termsUrl,
        string privacyUrl)
    {
        Id = Guid.NewGuid();
        IntegrationId = RequireId(integrationId, nameof(integrationId));
        DescriptionAr = RequireText(descriptionAr, nameof(descriptionAr));
        DescriptionEn = RequireText(descriptionEn, nameof(descriptionEn));
        LogoReference = SafeLogoReference(logoReference);
        TermsUrl = SafePublicUrl(termsUrl, nameof(termsUrl));
        PrivacyUrl = SafePublicUrl(privacyUrl, nameof(privacyUrl));
        ConfigurationVersion = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationId { get; private set; }
    public PlatformIntegrationConfiguration Integration { get; private set; } = null!;
    public string DescriptionAr { get; private set; } = string.Empty;
    public string DescriptionEn { get; private set; } = string.Empty;
    public string? LogoReference { get; private set; }
    public string TermsUrl { get; private set; } = string.Empty;
    public string PrivacyUrl { get; private set; } = string.Empty;
    public int ConfigurationVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(
        string descriptionAr,
        string descriptionEn,
        string? logoReference,
        string termsUrl,
        string privacyUrl)
    {
        DescriptionAr = RequireText(descriptionAr, nameof(descriptionAr));
        DescriptionEn = RequireText(descriptionEn, nameof(descriptionEn));
        LogoReference = SafeLogoReference(logoReference);
        TermsUrl = SafePublicUrl(termsUrl, nameof(termsUrl));
        PrivacyUrl = SafePublicUrl(privacyUrl, nameof(privacyUrl));
        BumpConfigurationVersion();
    }

    public void BumpConfigurationVersion()
    {
        checked
        {
            ConfigurationVersion++;
        }

        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static Guid RequireId(Guid value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier is required.", parameterName) : value;

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();

    private static string? OptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? SafeLogoReference(string? value)
    {
        var normalized = OptionalText(value);
        if (normalized is null)
        {
            return null;
        }

        if (!IsSafeApplicationRelativePath(normalized))
        {
            throw new ArgumentException(
                "Logo reference must be a safe application-relative path.",
                nameof(value));
        }

        return normalized;
    }

    private static string SafePublicUrl(string value, string parameterName)
    {
        var normalized = RequireText(value, parameterName);
        if (IsSafeApplicationRelativePath(normalized))
        {
            return normalized;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            ContainsTraversal(normalized))
        {
            throw new ArgumentException(
                "URL must be absolute HTTPS or a safe application-relative path.",
                parameterName);
        }

        return normalized;
    }

    private static bool IsSafeApplicationRelativePath(string value) =>
        value.StartsWith('/') &&
        !value.StartsWith("//", StringComparison.Ordinal) &&
        !value.Contains('\\') &&
        !value.Contains('#') &&
        !ContainsTraversal(value);

    private static bool ContainsTraversal(string value)
    {
        try
        {
            var decoded = Uri.UnescapeDataString(value);
            return decoded.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment is "." or "..");
        }
        catch (UriFormatException)
        {
            return true;
        }
    }
}

public sealed class CourierService
{
    private CourierService()
    {
    }

    public CourierService(
        Guid integrationId,
        string code,
        string nameAr,
        string nameEn,
        string descriptionAr,
        string descriptionEn,
        int sortOrder,
        int minimumPickupLeadTimeMinutes,
        TimeOnly dailyCutoffLocalTime,
        int maximumFuturePickupDays,
        int acceptanceWindowMinutes,
        bool supportsScheduledPickup,
        bool supportsSameDayPickup,
        bool canCreatePickup,
        bool canQueryStatus,
        bool supportsWebhook,
        bool supportsPolling,
        bool supportsManualUpdates,
        bool supportsCancellationBeforePickup,
        bool supportsCourierAssignment,
        bool supportsProofOfPickup,
        bool supportsProofOfDelivery,
        bool supportsDropOffPoint,
        int maximumEnvelopeWeightGrams,
        decimal maximumEnvelopeLengthCm,
        decimal maximumEnvelopeWidthCm,
        decimal maximumEnvelopeHeightCm)
    {
        if (supportsDropOffPoint)
        {
            throw new ArgumentException(
                "Drop-off points are not supported by the HomePickup service.",
                nameof(supportsDropOffPoint));
        }

        Id = Guid.NewGuid();
        IntegrationId = RequireId(integrationId, nameof(integrationId));
        ServiceType = CourierServiceType.HomePickup;
        Code = NormalizeCode(code);
        NameAr = RequireText(nameAr, nameof(nameAr));
        NameEn = RequireText(nameEn, nameof(nameEn));
        DescriptionAr = RequireText(descriptionAr, nameof(descriptionAr));
        DescriptionEn = RequireText(descriptionEn, nameof(descriptionEn));
        SortOrder = sortOrder;
        MinimumPickupLeadTimeMinutes = Positive(minimumPickupLeadTimeMinutes, nameof(minimumPickupLeadTimeMinutes));
        DailyCutoffLocalTime = dailyCutoffLocalTime;
        MaximumFuturePickupDays = Positive(maximumFuturePickupDays, nameof(maximumFuturePickupDays));
        AcceptanceWindowMinutes = Positive(acceptanceWindowMinutes, nameof(acceptanceWindowMinutes));
        SupportsScheduledPickup = supportsScheduledPickup;
        SupportsSameDayPickup = supportsSameDayPickup;
        CanCreatePickup = canCreatePickup;
        CanQueryStatus = canQueryStatus;
        SupportsWebhook = supportsWebhook;
        SupportsPolling = supportsPolling;
        SupportsManualUpdates = supportsManualUpdates;
        SupportsCancellationBeforePickup = supportsCancellationBeforePickup;
        SupportsCourierAssignment = supportsCourierAssignment;
        SupportsProofOfPickup = supportsProofOfPickup;
        SupportsProofOfDelivery = supportsProofOfDelivery;
        SupportsDropOffPoint = supportsDropOffPoint;
        MaximumEnvelopeWeightGrams = Positive(maximumEnvelopeWeightGrams, nameof(maximumEnvelopeWeightGrams));
        MaximumEnvelopeLengthCm = Positive(maximumEnvelopeLengthCm, nameof(maximumEnvelopeLengthCm));
        MaximumEnvelopeWidthCm = Positive(maximumEnvelopeWidthCm, nameof(maximumEnvelopeWidthCm));
        MaximumEnvelopeHeightCm = Positive(maximumEnvelopeHeightCm, nameof(maximumEnvelopeHeightCm));
        IsActive = false;
        Version = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationId { get; private set; }
    public PlatformIntegrationConfiguration Integration { get; private set; } = null!;
    public CourierServiceType ServiceType { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string DescriptionAr { get; private set; } = string.Empty;
    public string DescriptionEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public int MinimumPickupLeadTimeMinutes { get; private set; }
    public TimeOnly DailyCutoffLocalTime { get; private set; }
    public int MaximumFuturePickupDays { get; private set; }
    public int AcceptanceWindowMinutes { get; private set; }
    public bool SupportsScheduledPickup { get; private set; }
    public bool SupportsSameDayPickup { get; private set; }
    public bool CanCreatePickup { get; private set; }
    public bool CanQueryStatus { get; private set; }
    public bool SupportsWebhook { get; private set; }
    public bool SupportsPolling { get; private set; }
    public bool SupportsManualUpdates { get; private set; }
    public bool SupportsCancellationBeforePickup { get; private set; }
    public bool SupportsCourierAssignment { get; private set; }
    public bool SupportsProofOfPickup { get; private set; }
    public bool SupportsProofOfDelivery { get; private set; }
    public bool SupportsDropOffPoint { get; private set; }
    public int MaximumEnvelopeWeightGrams { get; private set; }
    public decimal MaximumEnvelopeLengthCm { get; private set; }
    public decimal MaximumEnvelopeWidthCm { get; private set; }
    public decimal MaximumEnvelopeHeightCm { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Update(
        string code,
        string nameAr,
        string nameEn,
        string descriptionAr,
        string descriptionEn,
        int sortOrder,
        int minimumPickupLeadTimeMinutes,
        TimeOnly dailyCutoffLocalTime,
        int maximumFuturePickupDays,
        int acceptanceWindowMinutes,
        bool supportsScheduledPickup,
        bool supportsSameDayPickup,
        bool canCreatePickup,
        bool canQueryStatus,
        bool supportsWebhook,
        bool supportsPolling,
        bool supportsManualUpdates,
        bool supportsCancellationBeforePickup,
        bool supportsCourierAssignment,
        bool supportsProofOfPickup,
        bool supportsProofOfDelivery,
        bool supportsDropOffPoint,
        int maximumEnvelopeWeightGrams,
        decimal maximumEnvelopeLengthCm,
        decimal maximumEnvelopeWidthCm,
        decimal maximumEnvelopeHeightCm)
    {
        if (supportsDropOffPoint)
        {
            throw new ArgumentException(
                "Drop-off points are not supported by the HomePickup service.",
                nameof(supportsDropOffPoint));
        }

        Code = NormalizeCode(code);
        NameAr = RequireText(nameAr, nameof(nameAr));
        NameEn = RequireText(nameEn, nameof(nameEn));
        DescriptionAr = RequireText(descriptionAr, nameof(descriptionAr));
        DescriptionEn = RequireText(descriptionEn, nameof(descriptionEn));
        SortOrder = sortOrder;
        MinimumPickupLeadTimeMinutes = Positive(minimumPickupLeadTimeMinutes, nameof(minimumPickupLeadTimeMinutes));
        DailyCutoffLocalTime = dailyCutoffLocalTime;
        MaximumFuturePickupDays = Positive(maximumFuturePickupDays, nameof(maximumFuturePickupDays));
        AcceptanceWindowMinutes = Positive(acceptanceWindowMinutes, nameof(acceptanceWindowMinutes));
        SupportsScheduledPickup = supportsScheduledPickup;
        SupportsSameDayPickup = supportsSameDayPickup;
        CanCreatePickup = canCreatePickup;
        CanQueryStatus = canQueryStatus;
        SupportsWebhook = supportsWebhook;
        SupportsPolling = supportsPolling;
        SupportsManualUpdates = supportsManualUpdates;
        SupportsCancellationBeforePickup = supportsCancellationBeforePickup;
        SupportsCourierAssignment = supportsCourierAssignment;
        SupportsProofOfPickup = supportsProofOfPickup;
        SupportsProofOfDelivery = supportsProofOfDelivery;
        SupportsDropOffPoint = supportsDropOffPoint;
        MaximumEnvelopeWeightGrams = Positive(maximumEnvelopeWeightGrams, nameof(maximumEnvelopeWeightGrams));
        MaximumEnvelopeLengthCm = Positive(maximumEnvelopeLengthCm, nameof(maximumEnvelopeLengthCm));
        MaximumEnvelopeWidthCm = Positive(maximumEnvelopeWidthCm, nameof(maximumEnvelopeWidthCm));
        MaximumEnvelopeHeightCm = Positive(maximumEnvelopeHeightCm, nameof(maximumEnvelopeHeightCm));
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static Guid RequireId(Guid value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier is required.", parameterName) : value;

    private static int Positive(int value, string parameterName) =>
        value <= 0 ? throw new ArgumentOutOfRangeException(parameterName) : value;

    private static decimal Positive(decimal value, string parameterName) =>
        value <= 0 ? throw new ArgumentOutOfRangeException(parameterName) : value;

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();

    private static string NormalizeCode(string code) => RequireText(code, nameof(code)).ToUpperInvariant();
}

public sealed class CourierCoverageRule
{
    private CourierCoverageRule()
    {
    }

    public CourierCoverageRule(
        Guid integrationId,
        Guid? serviceId,
        Guid countryId,
        Guid? governorateId,
        Guid? cityId,
        Guid? districtId,
        CourierCoverageResult result,
        string? notesAr,
        string? notesEn)
    {
        if (!Enum.IsDefined(result))
        {
            throw new ArgumentOutOfRangeException(nameof(result));
        }

        ValidateGeography(governorateId, cityId, districtId);
        Id = Guid.NewGuid();
        IntegrationId = RequireId(integrationId, nameof(integrationId));
        ServiceId = OptionalId(serviceId, nameof(serviceId));
        CountryId = RequireId(countryId, nameof(countryId));
        GovernorateId = OptionalId(governorateId, nameof(governorateId));
        CityId = OptionalId(cityId, nameof(cityId));
        DistrictId = OptionalId(districtId, nameof(districtId));
        Result = result;
        ScopeKey = CourierScopeKeys.Geography(CountryId, GovernorateId, CityId, DistrictId);
        NotesAr = OptionalText(notesAr);
        NotesEn = OptionalText(notesEn);
        IsActive = true;
        Version = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationId { get; private set; }
    public PlatformIntegrationConfiguration Integration { get; private set; } = null!;
    public Guid? ServiceId { get; private set; }
    public CourierService? Service { get; private set; }
    public Guid CountryId { get; private set; }
    public Country Country { get; private set; } = null!;
    public Guid? GovernorateId { get; private set; }
    public Governorate? Governorate { get; private set; }
    public Guid? CityId { get; private set; }
    public City? City { get; private set; }
    public Guid? DistrictId { get; private set; }
    public District? District { get; private set; }
    public CourierCoverageResult Result { get; private set; }
    public string ScopeKey { get; private set; } = string.Empty;
    public string? NotesAr { get; private set; }
    public string? NotesEn { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Update(
        CourierCoverageResult result,
        string? notesAr,
        string? notesEn)
    {
        if (!Enum.IsDefined(result))
        {
            throw new ArgumentOutOfRangeException(nameof(result));
        }

        Result = result;
        NotesAr = OptionalText(notesAr);
        NotesEn = OptionalText(notesEn);
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void ValidateGeography(Guid? governorateId, Guid? cityId, Guid? districtId)
    {
        if (cityId.HasValue && !governorateId.HasValue)
        {
            throw new ArgumentException("A city scope requires a governorate.", nameof(cityId));
        }

        if (districtId.HasValue && !cityId.HasValue)
        {
            throw new ArgumentException("A district scope requires a city.", nameof(districtId));
        }
    }

    private static Guid RequireId(Guid value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier is required.", parameterName) : value;

    private static Guid? OptionalId(Guid? value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier cannot be empty.", parameterName) : value;

    private static string? OptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CourierOperatingWindow
{
    private CourierOperatingWindow()
    {
    }

    public CourierOperatingWindow(
        Guid integrationId,
        Guid? serviceId,
        Guid? coverageRuleId,
        string scopeKey,
        string timeZoneId,
        DayOfWeek dayOfWeek,
        TimeOnly localStartTime,
        TimeOnly localEndTime)
    {
        if (!Enum.IsDefined(dayOfWeek))
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek));
        }

        if (localEndTime <= localStartTime)
        {
            throw new ArgumentException("Window end must be after its start.", nameof(localEndTime));
        }

        Id = Guid.NewGuid();
        IntegrationId = RequireId(integrationId, nameof(integrationId));
        ServiceId = OptionalId(serviceId, nameof(serviceId));
        CoverageRuleId = OptionalId(coverageRuleId, nameof(coverageRuleId));
        ScopeKey = RequireText(scopeKey, nameof(scopeKey)).ToLowerInvariant();
        TimeZoneId = RequireText(timeZoneId, nameof(timeZoneId));
        DayOfWeek = dayOfWeek;
        LocalStartTime = localStartTime;
        LocalEndTime = localEndTime;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationId { get; private set; }
    public PlatformIntegrationConfiguration Integration { get; private set; } = null!;
    public Guid? ServiceId { get; private set; }
    public CourierService? Service { get; private set; }
    public Guid? CoverageRuleId { get; private set; }
    public CourierCoverageRule? CoverageRule { get; private set; }
    public string ScopeKey { get; private set; } = string.Empty;
    public string TimeZoneId { get; private set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly LocalStartTime { get; private set; }
    public TimeOnly LocalEndTime { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Update(string timeZoneId, DayOfWeek dayOfWeek, TimeOnly localStartTime, TimeOnly localEndTime)
    {
        if (!Enum.IsDefined(dayOfWeek))
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek));
        }

        if (localEndTime <= localStartTime)
        {
            throw new ArgumentException("Window end must be after its start.", nameof(localEndTime));
        }

        TimeZoneId = RequireText(timeZoneId, nameof(timeZoneId));
        DayOfWeek = dayOfWeek;
        LocalStartTime = localStartTime;
        LocalEndTime = localEndTime;
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static Guid RequireId(Guid value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier is required.", parameterName) : value;

    private static Guid? OptionalId(Guid? value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier cannot be empty.", parameterName) : value;

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();
}

public sealed class CourierSlaDefinition
{
    private CourierSlaDefinition()
    {
    }

    public CourierSlaDefinition(
        Guid integrationId,
        Guid? serviceId,
        Guid? coverageRuleId,
        string scopeKey,
        int acceptanceTargetMinutes,
        int pickupSchedulingTargetMinutes,
        int pickupCompletionTargetMinutes,
        int deliveryToSchoolTargetMinutes,
        int? receiptConfirmationTargetMinutes)
    {
        Id = Guid.NewGuid();
        IntegrationId = RequireId(integrationId, nameof(integrationId));
        ServiceId = OptionalId(serviceId, nameof(serviceId));
        CoverageRuleId = OptionalId(coverageRuleId, nameof(coverageRuleId));
        ScopeKey = RequireText(scopeKey, nameof(scopeKey)).ToLowerInvariant();
        AcceptanceTargetMinutes = Positive(acceptanceTargetMinutes, nameof(acceptanceTargetMinutes));
        PickupSchedulingTargetMinutes = Positive(pickupSchedulingTargetMinutes, nameof(pickupSchedulingTargetMinutes));
        PickupCompletionTargetMinutes = Positive(pickupCompletionTargetMinutes, nameof(pickupCompletionTargetMinutes));
        DeliveryToSchoolTargetMinutes = Positive(deliveryToSchoolTargetMinutes, nameof(deliveryToSchoolTargetMinutes));
        ReceiptConfirmationTargetMinutes = OptionalPositive(
            receiptConfirmationTargetMinutes,
            nameof(receiptConfirmationTargetMinutes));
        IsActive = true;
        Version = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationId { get; private set; }
    public PlatformIntegrationConfiguration Integration { get; private set; } = null!;
    public Guid? ServiceId { get; private set; }
    public CourierService? Service { get; private set; }
    public Guid? CoverageRuleId { get; private set; }
    public CourierCoverageRule? CoverageRule { get; private set; }
    public string ScopeKey { get; private set; } = string.Empty;
    /// <summary>Elapsed-clock target minutes; not business minutes.</summary>
    public int AcceptanceTargetMinutes { get; private set; }
    /// <summary>Elapsed-clock target minutes; not business minutes.</summary>
    public int PickupSchedulingTargetMinutes { get; private set; }
    /// <summary>Elapsed-clock target minutes; not business minutes.</summary>
    public int PickupCompletionTargetMinutes { get; private set; }
    /// <summary>Elapsed-clock target minutes; not business minutes.</summary>
    public int DeliveryToSchoolTargetMinutes { get; private set; }
    /// <summary>Optional elapsed-clock target minutes; not business minutes.</summary>
    public int? ReceiptConfirmationTargetMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Update(
        int acceptanceTargetMinutes,
        int pickupSchedulingTargetMinutes,
        int pickupCompletionTargetMinutes,
        int deliveryToSchoolTargetMinutes,
        int? receiptConfirmationTargetMinutes)
    {
        AcceptanceTargetMinutes = Positive(acceptanceTargetMinutes, nameof(acceptanceTargetMinutes));
        PickupSchedulingTargetMinutes = Positive(pickupSchedulingTargetMinutes, nameof(pickupSchedulingTargetMinutes));
        PickupCompletionTargetMinutes = Positive(pickupCompletionTargetMinutes, nameof(pickupCompletionTargetMinutes));
        DeliveryToSchoolTargetMinutes = Positive(deliveryToSchoolTargetMinutes, nameof(deliveryToSchoolTargetMinutes));
        ReceiptConfirmationTargetMinutes = OptionalPositive(
            receiptConfirmationTargetMinutes,
            nameof(receiptConfirmationTargetMinutes));
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static Guid RequireId(Guid value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier is required.", parameterName) : value;

    private static Guid? OptionalId(Guid? value, string parameterName) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier cannot be empty.", parameterName) : value;

    private static int Positive(int value, string parameterName) =>
        value <= 0 ? throw new ArgumentOutOfRangeException(parameterName) : value;

    private static int? OptionalPositive(int? value, string parameterName) =>
        value is <= 0 ? throw new ArgumentOutOfRangeException(parameterName) : value;

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();
}

public sealed class CourierHealthCheckRecord
{
    private CourierHealthCheckRecord()
    {
    }

    public CourierHealthCheckRecord(
        Guid integrationId,
        IntegrationHealthStatus status,
        string? safeCode,
        DateTimeOffset checkedAtUtc,
        bool isAvailable,
        bool isOperational)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Id = Guid.NewGuid();
        IntegrationId = integrationId == Guid.Empty
            ? throw new ArgumentException("Identifier is required.", nameof(integrationId))
            : integrationId;
        Status = status;
        var normalizedCode = string.IsNullOrWhiteSpace(safeCode) ? null : safeCode.Trim();
        SafeCode = normalizedCode is null
            ? null
            : normalizedCode[..Math.Min(normalizedCode.Length, 100)];
        CheckedAtUtc = checkedAtUtc;
        IsAvailable = isAvailable;
        IsOperational = isOperational;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationId { get; private set; }
    public PlatformIntegrationConfiguration Integration { get; private set; } = null!;
    public IntegrationHealthStatus Status { get; private set; }
    public string? SafeCode { get; private set; }
    public DateTimeOffset CheckedAtUtc { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsOperational { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public static class CourierScopeKeys
{
    public static string Geography(
        Guid countryId,
        Guid? governorateId = null,
        Guid? cityId = null,
        Guid? districtId = null) =>
        $"country:{Token(countryId)}|governorate:{Token(governorateId)}|city:{Token(cityId)}|district:{Token(districtId)}";

    private static string Token(Guid? value) => value.HasValue ? value.Value.ToString("N") : "*";
}
