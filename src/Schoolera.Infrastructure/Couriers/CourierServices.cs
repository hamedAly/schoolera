using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Couriers;
using Schoolera.Application.Integrations;
using Schoolera.Application.Meetings;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Couriers;

public sealed class SimulatedCourierProvider(IRuntimeEnvironment runtimeEnvironment) : ICourierProvider
{
    public string ProviderCode => IntegrationProviderCodes.Simulated;
    public bool IsSimulated => true;

    public Task<CourierHealthResult> CheckHealthAsync(
        CourierIntegrationSettings settings,
        CancellationToken cancellationToken = default)
    {
        EnsureAllowed(settings);
        cancellationToken.ThrowIfCancellationRequested();
        var scenario = ParseHealth(settings.HealthScenario);
        var status = scenario switch
        {
            SimulatedCourierHealthScenario.Healthy => IntegrationHealthStatus.Healthy,
            SimulatedCourierHealthScenario.Degraded => IntegrationHealthStatus.Degraded,
            _ => IntegrationHealthStatus.Unhealthy,
        };
        var safeCode = scenario == SimulatedCourierHealthScenario.Healthy
            ? null
            : $"courier.simulated.health.{scenario.ToString().ToLowerInvariant()}";
        return Task.FromResult(new CourierHealthResult(
            status,
            safeCode,
            DateTimeOffset.UtcNow,
            status == IntegrationHealthStatus.Healthy,
            status == IntegrationHealthStatus.Healthy));
    }

    public Task<bool> IsAvailableAsync(
        CourierIntegrationSettings settings,
        CancellationToken cancellationToken = default)
    {
        EnsureAllowed(settings);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ParseAvailability(settings.AvailabilityScenario) ==
                               SimulatedCourierAvailabilityScenario.Available);
    }

    private void EnsureAllowed(CourierIntegrationSettings settings)
    {
        if ((!runtimeEnvironment.IsDevelopment && !runtimeEnvironment.IsEnvironment("Test")) ||
            !Enum.TryParse<CourierProviderEnvironment>(settings.Environment, true, out var environment) ||
            environment != CourierProviderEnvironment.Simulated)
        {
            throw new InvalidOperationException(
                "The simulated courier provider is available only in Development/Test with Simulated settings.");
        }
    }

    private static SimulatedCourierHealthScenario ParseHealth(string value) =>
        Enum.TryParse<SimulatedCourierHealthScenario>(value, true, out var scenario) &&
        Enum.IsDefined(scenario)
            ? scenario
            : throw new InvalidOperationException("Unsupported simulated courier health scenario.");

    private static SimulatedCourierAvailabilityScenario ParseAvailability(string value) =>
        Enum.TryParse<SimulatedCourierAvailabilityScenario>(value, true, out var scenario) &&
        Enum.IsDefined(scenario)
            ? scenario
            : throw new InvalidOperationException("Unsupported simulated courier availability scenario.");
}

public sealed class CourierConfigurationResolver(
    SchooleraDbContext dbContext,
    IIntegrationSettingsValidator settingsValidator,
    IRuntimeEnvironment runtimeEnvironment) : ICourierConfigurationResolver
{
    public async Task<CourierResolvedConfiguration?> ResolveOperationalAsync(
        CancellationToken cancellationToken = default)
    {
        if (!runtimeEnvironment.IsDevelopment && !runtimeEnvironment.IsEnvironment("Test"))
        {
            return null;
        }

        var candidates = await dbContext.PlatformIntegrationConfigurations
            .AsNoTracking()
            .Where(x => x.IntegrationType == IntegrationType.Courier && x.IsActive && x.IsDefault)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var integration in candidates)
        {
            if (!IsOperationalProvider(integration.ProviderCode))
            {
                continue;
            }

            var validation = settingsValidator.Validate(
                IntegrationType.Courier,
                integration.ProviderCode,
                integration.SettingsJson,
                integration.SettingsSchemaVersion);
            if (!validation.IsValid)
            {
                continue;
            }

            CourierIntegrationSettings settings;
            try
            {
                settings = IntegrationSettingsSerializer.Deserialize<CourierIntegrationSettings>(
                    integration.SettingsJson);
            }
            catch
            {
                continue;
            }

            if (!Enum.TryParse<CourierProviderEnvironment>(settings.Environment, true, out var environment) ||
                environment != CourierProviderEnvironment.Simulated)
            {
                continue;
            }

            var profile = await dbContext.CourierProviderProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.IntegrationId == integration.Id, cancellationToken);
            if (profile is not null)
            {
                return new CourierResolvedConfiguration(integration, profile, settings);
            }
        }

        return null;
    }

    private static bool IsOperationalProvider(string providerCode) =>
        string.Equals(providerCode, IntegrationProviderCodes.Simulated, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(providerCode, IntegrationProviderCodes.Development, StringComparison.OrdinalIgnoreCase);
}

public sealed class CourierLocationHierarchyValidator(SchooleraDbContext dbContext)
    : ICourierLocationHierarchyValidator
{
    public async Task<ValidatedCourierLocation?> ValidateAsync(
        CourierLocation location,
        CancellationToken cancellationToken = default)
    {
        if (location.CountryId == Guid.Empty ||
            location.GovernorateId == Guid.Empty ||
            location.CityId == Guid.Empty ||
            location.DistrictId == Guid.Empty ||
            location.CityId.HasValue && !location.GovernorateId.HasValue ||
            location.DistrictId.HasValue && !location.CityId.HasValue)
        {
            return null;
        }

        if (!await dbContext.Countries.AsNoTracking()
                .AnyAsync(x => x.Id == location.CountryId && x.IsActive, cancellationToken))
        {
            return null;
        }

        if (location.GovernorateId.HasValue &&
            !await dbContext.Governorates.AsNoTracking().AnyAsync(
                x => x.Id == location.GovernorateId.Value &&
                     x.CountryId == location.CountryId &&
                     x.IsActive,
                cancellationToken))
        {
            return null;
        }

        if (location.CityId.HasValue &&
            !await dbContext.Cities.AsNoTracking().AnyAsync(
                x => x.Id == location.CityId.Value &&
                     x.GovernorateId == location.GovernorateId &&
                     x.IsActive,
                cancellationToken))
        {
            return null;
        }

        if (location.DistrictId.HasValue &&
            !await dbContext.Districts.AsNoTracking().AnyAsync(
                x => x.Id == location.DistrictId.Value &&
                     x.CityId == location.CityId &&
                     x.IsActive,
                cancellationToken))
        {
            return null;
        }

        var scopes = new List<string>
        {
            CourierScopeKeys.Geography(location.CountryId),
        };
        if (location.GovernorateId.HasValue)
            scopes.Add(CourierScopeKeys.Geography(location.CountryId, location.GovernorateId));
        if (location.CityId.HasValue)
            scopes.Add(CourierScopeKeys.Geography(
                location.CountryId, location.GovernorateId, location.CityId));
        if (location.DistrictId.HasValue)
            scopes.Add(CourierScopeKeys.Geography(
                location.CountryId, location.GovernorateId, location.CityId, location.DistrictId));

        return new ValidatedCourierLocation(
            location.CountryId,
            location.GovernorateId,
            location.CityId,
            location.DistrictId,
            scopes);
    }
}

public sealed class CourierHealthService(
    SchooleraDbContext dbContext,
    ICourierConfigurationResolver configurationResolver,
    IEnumerable<ICourierProvider> providers,
    IIntegrationSettingsValidator settingsValidator,
    INotificationOutboxPublisher notificationPublisher,
    IOptions<CourierOptions> options,
    IUnitOfWork unitOfWork) : ICourierHealthService
{
    public async Task<CourierHealthResult?> CheckAndRecordAsync(
        Guid? integrationId = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = integrationId.HasValue
            ? await ResolveForTestAsync(integrationId.Value, cancellationToken)
            : await configurationResolver.ResolveOperationalAsync(cancellationToken);
        if (resolved is null)
        {
            return null;
        }

        var provider = ResolveProvider(providers, resolved.Integration.ProviderCode);
        if (provider is null)
        {
            return null;
        }

        var result = await provider.CheckHealthAsync(resolved.Settings, cancellationToken);
        var threshold = Math.Clamp(options.Value.HealthFailureAlertThreshold, 1, 100);
        var previousStatuses = await dbContext.CourierHealthCheckRecords
            .AsNoTracking()
            .Where(x => x.IntegrationId == resolved.Integration.Id)
            .OrderByDescending(x => x.CheckedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(threshold)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var shouldAlert = CourierHealthAlertPolicy.ShouldAlert(result.Status, previousStatuses, threshold);

        var tracked = await dbContext.PlatformIntegrationConfigurations
            .SingleAsync(x => x.Id == resolved.Integration.Id, cancellationToken);
        tracked.RecordHealth(result.Status, result.SafeCode);
        dbContext.CourierHealthCheckRecords.Add(new CourierHealthCheckRecord(
            tracked.Id,
            result.Status,
            result.SafeCode,
            result.CheckedAtUtc,
            result.IsAvailable,
            result.IsOperational));

        if (shouldAlert)
        {
            await EnqueuePlatformAdminAlertsAsync(tracked, result, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task EnqueuePlatformAdminAlertsAsync(
        PlatformIntegrationConfiguration integration,
        CourierHealthResult result,
        CancellationToken cancellationToken)
    {
        var lastHealthyRecordId = await dbContext.CourierHealthCheckRecords
            .AsNoTracking()
            .Where(x => x.IntegrationId == integration.Id &&
                        x.Status == IntegrationHealthStatus.Healthy)
            .OrderByDescending(x => x.CheckedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var administrators = await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where role.Name == SchooleraRoles.PlatformAdmin &&
                      user.AccountStatus == AccountStatus.Active
                select new { user.Id, user.PreferredLanguage })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var administrator in administrators)
        {
            var culture = string.Equals(administrator.PreferredLanguage, "en", StringComparison.OrdinalIgnoreCase)
                ? "en"
                : "ar";
            var providerName = culture == "en" && !string.IsNullOrWhiteSpace(integration.DisplayNameEn)
                ? integration.DisplayNameEn
                : integration.DisplayNameAr;
            var incidentKey = lastHealthyRecordId?.ToString("N") ?? "initial";
            await notificationPublisher.EnqueueAsync(
                new NotificationEnqueueRequest(
                    administrator.Id,
                    NotificationEventType.CourierHealthIncident,
                    culture,
                    $"courier-health:{integration.Id:N}:incident:{incidentKey}:admin:{administrator.Id:N}",
                    new Dictionary<string, string>
                    {
                        ["providerName"] = providerName,
                        ["providerCode"] = integration.ProviderCode,
                        ["status"] = result.Status.ToString(),
                        ["safeCode"] = string.IsNullOrWhiteSpace(result.SafeCode) ? "courier.health.failed" : result.SafeCode,
                    },
                    ActionPath: "/admin/integrations",
                    RelatedEntityId: integration.Id,
                    ForceChannels: [NotificationChannel.InApp],
                    SkipPreferenceCheck: true),
                cancellationToken);
        }
    }

    private async Task<CourierResolvedConfiguration?> ResolveForTestAsync(
        Guid integrationId,
        CancellationToken cancellationToken)
    {
        var integration = await dbContext.PlatformIntegrationConfigurations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == integrationId && x.IntegrationType == IntegrationType.Courier,
                cancellationToken);
        if (integration is null ||
            !settingsValidator.Validate(
                integration.IntegrationType,
                integration.ProviderCode,
                integration.SettingsJson,
                integration.SettingsSchemaVersion).IsValid)
        {
            return null;
        }

        var profile = await dbContext.CourierProviderProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IntegrationId == integrationId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        try
        {
            return new CourierResolvedConfiguration(
                integration,
                profile,
                IntegrationSettingsSerializer.Deserialize<CourierIntegrationSettings>(integration.SettingsJson));
        }
        catch
        {
            return null;
        }
    }

    internal static ICourierProvider? ResolveProvider(
        IEnumerable<ICourierProvider> providers,
        string providerCode) =>
        providers.FirstOrDefault(x =>
            string.Equals(x.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase) ||
            x.IsSimulated &&
            string.Equals(providerCode, IntegrationProviderCodes.Development, StringComparison.OrdinalIgnoreCase));
}

public sealed class CourierAvailabilityResolver(
    SchooleraDbContext dbContext,
    ICourierConfigurationResolver configurationResolver,
    ICourierLocationHierarchyValidator hierarchyValidator,
    IEnumerable<ICourierProvider> providers,
    IOptions<CourierOptions> options) : ICourierAvailabilityResolver
{
    public async Task<Result<IReadOnlyList<CourierAvailabilityOptionDto>>> ResolveAsync(
        CourierAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await hierarchyValidator.ValidateAsync(request.Location, cancellationToken);
        if (location is null)
        {
            return Result<IReadOnlyList<CourierAvailabilityOptionDto>>.Failure(
                ["The courier location hierarchy is invalid."],
                ["courier.invalidLocation"]);
        }

        var configuration = await configurationResolver.ResolveOperationalAsync(cancellationToken);
        if (configuration is null || !HasFreshHealthyState(configuration.Integration))
        {
            return Result<IReadOnlyList<CourierAvailabilityOptionDto>>.Success([]);
        }

        var provider = CourierHealthService.ResolveProvider(providers, configuration.Integration.ProviderCode);
        if (provider is null ||
            !await provider.IsAvailableAsync(configuration.Settings, cancellationToken))
        {
            return Result<IReadOnlyList<CourierAvailabilityOptionDto>>.Success([]);
        }

        var services = await dbContext.CourierServices.AsNoTracking()
            .Where(x => x.IntegrationId == configuration.Integration.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var rules = await dbContext.CourierCoverageRules.AsNoTracking()
            .Where(x => x.IntegrationId == configuration.Integration.Id && x.IsActive)
            .ToListAsync(cancellationToken);
        var windows = await dbContext.CourierOperatingWindows.AsNoTracking()
            .Where(x => x.IntegrationId == configuration.Integration.Id && x.IsActive)
            .ToListAsync(cancellationToken);
        var slas = await dbContext.CourierSlaDefinitions.AsNoTracking()
            .Where(x => x.IntegrationId == configuration.Integration.Id && x.IsActive)
            .ToListAsync(cancellationToken);

        var result = new List<CourierAvailabilityOptionDto>();
        foreach (var service in services)
        {
            var rule = SelectCoverage(rules, service.Id, location);
            if (rule is null || rule.Result != CourierCoverageResult.Covered)
            {
                continue;
            }

            var window = SelectWindows(windows, service.Id, rule.Id, location.ScopeKeys);
            var earliest = EarliestPickup(request.RequestedAtUtc, service, window);
            if (earliest is null)
            {
                continue;
            }

            var sla = SelectSla(slas, service.Id, rule.Id);
            result.Add(new CourierAvailabilityOptionDto(
                configuration.Integration.ProviderCode,
                configuration.Integration.DisplayNameAr,
                configuration.Integration.DisplayNameEn,
                configuration.Profile.DescriptionAr,
                configuration.Profile.DescriptionEn,
                service.Code,
                service.NameAr,
                service.NameEn,
                service.DescriptionAr,
                service.DescriptionEn,
                earliest.Value,
                window[0].TimeZoneId,
                configuration.Profile.TermsUrl,
                configuration.Profile.PrivacyUrl,
                configuration.Profile.LogoReference,
                provider.IsSimulated,
                rule.NotesAr,
                rule.NotesEn,
                EffectiveCapabilities(service),
                sla is null
                    ? null
                    : new CourierSlaDto(
                        sla.AcceptanceTargetMinutes,
                        sla.PickupSchedulingTargetMinutes,
                        sla.PickupCompletionTargetMinutes,
                        sla.DeliveryToSchoolTargetMinutes,
                        sla.ReceiptConfirmationTargetMinutes)));
        }

        return Result<IReadOnlyList<CourierAvailabilityOptionDto>>.Success(result);
    }

    private bool HasFreshHealthyState(PlatformIntegrationConfiguration integration)
    {
        var now = DateTimeOffset.UtcNow;
        return CourierHealthFreshnessPolicy.IsFreshHealthy(
            integration.HealthStatus,
            integration.LastHealthCheckAtUtc,
            now,
            options.Value.HealthFreshnessMinutes);
    }

    public static CourierCoverageRule? SelectCoverage(
        IEnumerable<CourierCoverageRule> rules,
        Guid serviceId,
        ValidatedCourierLocation location) =>
        rules.Where(x =>
                (x.ServiceId is null || x.ServiceId == serviceId) &&
                x.CountryId == location.CountryId &&
                (!x.GovernorateId.HasValue || x.GovernorateId == location.GovernorateId) &&
                (!x.CityId.HasValue || x.CityId == location.CityId) &&
                (!x.DistrictId.HasValue || x.DistrictId == location.DistrictId))
            .OrderByDescending(x => CoverageSpecificity(x))
            .ThenByDescending(x => x.ServiceId.HasValue)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

    private static int CoverageSpecificity(CourierCoverageRule rule) =>
        rule.DistrictId.HasValue ? 4 :
        rule.CityId.HasValue ? 3 :
        rule.GovernorateId.HasValue ? 2 : 1;

    private static IReadOnlyList<CourierOperatingWindow> SelectWindows(
        IEnumerable<CourierOperatingWindow> windows,
        Guid serviceId,
        Guid coverageRuleId,
        IReadOnlyList<string> scopeKeys)
    {
        var matching = windows.Where(x =>
                (x.ServiceId is null || x.ServiceId == serviceId) &&
                (x.CoverageRuleId is null || x.CoverageRuleId == coverageRuleId) &&
                scopeKeys.Contains(x.ScopeKey, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var bestRank = matching
            .Select(x => WindowRank(x, coverageRuleId, scopeKeys))
            .DefaultIfEmpty(-1)
            .Max();
        return matching
            .Where(x => WindowRank(x, coverageRuleId, scopeKeys) == bestRank)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.LocalStartTime)
            .ToList();
    }

    private static int WindowRank(
        CourierOperatingWindow window,
        Guid coverageRuleId,
        IReadOnlyList<string> scopeKeys)
    {
        var coverage = window.CoverageRuleId == coverageRuleId ? 100 : window.CoverageRuleId is null ? 0 : -1000;
        var service = window.ServiceId.HasValue ? 10 : 0;
        var geography = scopeKeys.ToList().FindIndex(x =>
            string.Equals(x, window.ScopeKey, StringComparison.OrdinalIgnoreCase));
        return coverage + service + geography;
    }

    private static CourierSlaDefinition? SelectSla(
        IEnumerable<CourierSlaDefinition> slas,
        Guid serviceId,
        Guid coverageRuleId) =>
        slas.Where(x =>
                x.CoverageRuleId == coverageRuleId &&
                (x.ServiceId is null || x.ServiceId == serviceId))
            .OrderByDescending(x => x.ServiceId == serviceId)
            .FirstOrDefault() ??
        slas.Where(x => x.CoverageRuleId is null && x.ServiceId == serviceId).OrderBy(x => x.Id).FirstOrDefault() ??
        slas.Where(x => x.CoverageRuleId is null && x.ServiceId is null).OrderBy(x => x.Id).FirstOrDefault();

    private static DateTimeOffset? EarliestPickup(
        DateTimeOffset requestedAtUtc,
        CourierService service,
        IReadOnlyList<CourierOperatingWindow> windows)
    {
        if (windows.Count == 0)
        {
            return null;
        }

        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(windows[0].TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }

        if (windows.Any(x => !string.Equals(x.TimeZoneId, zone.Id, StringComparison.Ordinal)))
        {
            return null;
        }

        var requestedLocal = TimeZoneInfo.ConvertTime(requestedAtUtc, zone);
        var leadUtc = requestedAtUtc.AddMinutes(service.MinimumPickupLeadTimeMinutes);
        var lastDayOffset = service.SupportsScheduledPickup ? service.MaximumFuturePickupDays : 0;
        for (var dayOffset = 0; dayOffset <= lastDayOffset; dayOffset++)
        {
            var date = DateOnly.FromDateTime(requestedLocal.Date).AddDays(dayOffset);
            if (dayOffset == 0 &&
                (!service.SupportsSameDayPickup ||
                 TimeOnly.FromDateTime(requestedLocal.DateTime) > service.DailyCutoffLocalTime))
            {
                continue;
            }

            foreach (var window in windows.Where(x => x.DayOfWeek == date.DayOfWeek))
            {
                var startLocal = date.ToDateTime(window.LocalStartTime, DateTimeKind.Unspecified);
                var endLocal = date.ToDateTime(window.LocalEndTime, DateTimeKind.Unspecified);
                if (zone.IsInvalidTime(startLocal) || zone.IsInvalidTime(endLocal))
                {
                    continue;
                }

                var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, zone), TimeSpan.Zero);
                var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocal, zone), TimeSpan.Zero);
                var candidate = startUtc > leadUtc ? startUtc : leadUtc;
                if (candidate < endUtc)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static CourierCapabilitiesDto EffectiveCapabilities(CourierService service) => new(
        CanCreatePickup: false,
        CanQueryStatus: false,
        SupportsWebhook: false,
        SupportsPolling: false,
        SupportsManualUpdates: false,
        SupportsCancellationBeforePickup: service.SupportsCancellationBeforePickup,
        SupportsCourierAssignment: false,
        SupportsProofOfPickup: false,
        SupportsProofOfDelivery: false,
        SupportsDropOffPoint: false);
}
