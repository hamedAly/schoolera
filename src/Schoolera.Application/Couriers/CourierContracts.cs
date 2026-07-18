using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Couriers;

public sealed class CourierOptions
{
    public const string SectionName = "Courier";
    public int HealthFreshnessMinutes { get; set; } = 60;
    public int HealthFailureAlertThreshold { get; set; } = 3;
}

public static class CourierHealthAlertPolicy
{
    public static bool ShouldAlert(
        IntegrationHealthStatus currentStatus,
        IReadOnlyList<IntegrationHealthStatus> previousStatuses,
        int threshold)
    {
        if (currentStatus is not (IntegrationHealthStatus.Degraded or IntegrationHealthStatus.Unhealthy))
        {
            return false;
        }

        threshold = Math.Clamp(threshold, 1, 100);
        var consecutiveFailures = 1;
        foreach (var status in previousStatuses)
        {
            if (status is not (IntegrationHealthStatus.Degraded or IntegrationHealthStatus.Unhealthy))
            {
                break;
            }

            consecutiveFailures++;
        }

        return consecutiveFailures == threshold;
    }
}

public static class CourierHealthFreshnessPolicy
{
    public static bool IsFreshHealthy(
        IntegrationHealthStatus status,
        DateTimeOffset? checkedAtUtc,
        DateTimeOffset nowUtc,
        int freshnessMinutes) =>
        status == IntegrationHealthStatus.Healthy &&
        checkedAtUtc.HasValue &&
        checkedAtUtc.Value <= nowUtc &&
        checkedAtUtc.Value >= nowUtc.AddMinutes(-Math.Clamp(freshnessMinutes, 1, 1440));
}

public sealed record CourierLocation(
    Guid CountryId,
    Guid? GovernorateId = null,
    Guid? CityId = null,
    Guid? DistrictId = null);

public sealed record ValidatedCourierLocation(
    Guid CountryId,
    Guid? GovernorateId,
    Guid? CityId,
    Guid? DistrictId,
    IReadOnlyList<string> ScopeKeys);

public sealed record CourierCapabilitiesDto(
    bool CanCreatePickup,
    bool CanQueryStatus,
    bool SupportsWebhook,
    bool SupportsPolling,
    bool SupportsManualUpdates,
    bool SupportsCancellationBeforePickup,
    bool SupportsCourierAssignment,
    bool SupportsProofOfPickup,
    bool SupportsProofOfDelivery,
    bool SupportsDropOffPoint);

public sealed record CourierSlaDto(
    int AcceptanceTargetMinutes,
    int PickupSchedulingTargetMinutes,
    int PickupCompletionTargetMinutes,
    int DeliveryToSchoolTargetMinutes,
    int? ReceiptConfirmationTargetMinutes);

public sealed record CourierAvailabilityOptionDto(
    string ProviderCode,
    string ProviderNameAr,
    string? ProviderNameEn,
    string ProviderDescriptionAr,
    string ProviderDescriptionEn,
    string ServiceCode,
    string ServiceNameAr,
    string ServiceNameEn,
    string ServiceDescriptionAr,
    string ServiceDescriptionEn,
    DateTimeOffset EarliestPickupAtUtc,
    string TimeZoneId,
    string TermsUrl,
    string PrivacyUrl,
    string? LogoReference,
    bool IsSimulatedWarning,
    string? CoverageNotesAr,
    string? CoverageNotesEn,
    CourierCapabilitiesDto Capabilities,
    CourierSlaDto? Sla);

public sealed record CourierAvailabilityRequest(
    CourierLocation Location,
    DateTimeOffset RequestedAtUtc);

public sealed record CourierHealthResult(
    IntegrationHealthStatus Status,
    string? SafeCode,
    DateTimeOffset CheckedAtUtc,
    bool IsAvailable,
    bool IsOperational);

public sealed record CourierResolvedConfiguration(
    PlatformIntegrationConfiguration Integration,
    CourierProviderProfile Profile,
    CourierIntegrationSettings Settings);

public interface ICourierProvider
{
    string ProviderCode { get; }
    bool IsSimulated { get; }
    Task<CourierHealthResult> CheckHealthAsync(
        CourierIntegrationSettings settings,
        CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(
        CourierIntegrationSettings settings,
        CancellationToken cancellationToken = default);
}

public interface ICourierConfigurationResolver
{
    Task<CourierResolvedConfiguration?> ResolveOperationalAsync(
        CancellationToken cancellationToken = default);
}

public interface ICourierLocationHierarchyValidator
{
    Task<ValidatedCourierLocation?> ValidateAsync(
        CourierLocation location,
        CancellationToken cancellationToken = default);
}

public interface ICourierAvailabilityResolver
{
    Task<Result<IReadOnlyList<CourierAvailabilityOptionDto>>> ResolveAsync(
        CourierAvailabilityRequest request,
        CancellationToken cancellationToken = default);
}

public interface ICourierHealthService
{
    Task<CourierHealthResult?> CheckAndRecordAsync(
        Guid? integrationId = null,
        CancellationToken cancellationToken = default);
}
