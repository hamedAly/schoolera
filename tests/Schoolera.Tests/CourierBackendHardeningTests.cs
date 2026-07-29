using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Couriers;
using Schoolera.Application.Couriers.Commands.MutateCourier;
using Schoolera.Application.Couriers.Queries.GetParentCourierAvailability;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Couriers;
using Schoolera.Application.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Schoolera.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Schoolera.Api.Controllers;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class CourierBackendHardeningTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public CourierBackendHardeningTests(SchooleraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void ProviderProfile_RejectsUnsafePublicUrls()
    {
        Assert.Throws<ArgumentException>(() => NewProfile("http://courier.example/terms", "/privacy"));
        Assert.Throws<ArgumentException>(() => NewProfile("https://user:secret@courier.example/terms", "/privacy"));
        Assert.Throws<ArgumentException>(() => NewProfile("https://courier.example/terms#fragment", "/privacy"));
        Assert.Throws<ArgumentException>(() => NewProfile("//courier.example/terms", "/privacy"));
        Assert.Throws<ArgumentException>(() => NewProfile("/legal/../secret", "/privacy"));
        Assert.Throws<ArgumentException>(() => NewProfile("/legal/%2e%2e/secret", "/privacy"));

        var profile = NewProfile("https://courier.example/terms", "/legal/privacy");
        Assert.Equal("https://courier.example/terms", profile.TermsUrl);
        Assert.Equal("/legal/privacy", profile.PrivacyUrl);
    }

    [Fact]
    public void HomePickupService_RejectsDropOffCapability()
    {
        Assert.Throws<ArgumentException>(() => new CourierService(
            Guid.NewGuid(), "HOME", "استلام منزلي", "Home pickup", "وصف", "Description",
            sortOrder: 1, minimumPickupLeadTimeMinutes: 30, dailyCutoffLocalTime: new TimeOnly(14, 0),
            maximumFuturePickupDays: 7, acceptanceWindowMinutes: 60,
            supportsScheduledPickup: true, supportsSameDayPickup: true,
            canCreatePickup: false, canQueryStatus: false, supportsWebhook: false,
            supportsPolling: false, supportsManualUpdates: false,
            supportsCancellationBeforePickup: true, supportsCourierAssignment: false,
            supportsProofOfPickup: false, supportsProofOfDelivery: false,
            supportsDropOffPoint: true, maximumEnvelopeWeightGrams: 1000,
            maximumEnvelopeLengthCm: 30, maximumEnvelopeWidthCm: 20,
            maximumEnvelopeHeightCm: 5));
    }

    [Fact]
    public void AvailabilityOption_ExposesSafeProfileFieldsWithoutSettingsOrInternalIds()
    {
        var properties = typeof(CourierAvailabilityOptionDto).GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(CourierAvailabilityOptionDto.LogoReference), properties);
        Assert.Contains(nameof(CourierAvailabilityOptionDto.TermsUrl), properties);
        Assert.Contains(nameof(CourierAvailabilityOptionDto.PrivacyUrl), properties);
        Assert.Contains(nameof(CourierAvailabilityOptionDto.IsSimulatedWarning), properties);
        Assert.DoesNotContain("SettingsJson", properties);
        Assert.DoesNotContain("IntegrationId", properties);
        Assert.DoesNotContain("ServiceId", properties);
        Assert.DoesNotContain("HealthStatus", properties);

        var adminProperties = typeof(CourierAdminDetailDto).GetProperties()
            .Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(nameof(CourierAdminDetailDto.ConfigurationVersion), adminProperties);
        Assert.Contains(nameof(CourierAdminDetailDto.ConcurrencyRowVersion), adminProperties);
        Assert.Contains(nameof(CourierServiceDto.ServiceType),
            typeof(CourierServiceDto).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public void MutationValidator_RejectsDropOffAndMalformedConcurrencyToken()
    {
        var validator = new MutateCourierCommandValidator();
        var body = NewServiceRequest(supportsDropOffPoint: true, expectedRowVersion: "not-base64");
        var result = validator.Validate(new MutateCourierCommand(
            Guid.NewGuid(), CourierMutationKind.CreateService, Service: body));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ParentAvailability_NonOwnedApplication_DoesNotResolveCourierOptions()
    {
        var parentId = Guid.NewGuid();
        var contextReader = new MissingDestinationReader();
        var resolver = new ThrowingAvailabilityResolver();
        var handler = new GetParentCourierAvailabilityQueryHandler(
            new ParentCurrentUser(parentId), contextReader, resolver);

        var result = await handler.Handle(
            new GetParentCourierAvailabilityQuery(Guid.NewGuid(), Guid.NewGuid(), null, null, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(CourierErrorCodes.NotFound, result.ErrorCodes);
        Assert.Equal(parentId, contextReader.RequestedParentUserId);
        Assert.False(resolver.WasCalled);
    }

    [Theory]
    [InlineData("https://user:secret@courier.example/api", "Production")]
    [InlineData("https://courier.example/api#fragment", "Production")]
    [InlineData("http://courier.example/api", "Production")]
    [InlineData("https://127.0.0.1/api", "Production")]
    [InlineData("https://10.1.2.3/api", "Production")]
    [InlineData("https://172.16.0.1/api", "Production")]
    [InlineData("https://192.168.1.1/api", "Production")]
    [InlineData("https://[::1]/api", "Production")]
    [InlineData("https://[fc00::1]/api", "Production")]
    public void CourierSettingsValidator_RejectsUnsafeApiBaseUrls(string apiBaseUrl, string environment)
    {
        var json = $$"""{"environment":"{{environment}}","apiBaseUrl":"{{apiBaseUrl}}","requestTimeoutSeconds":10,"maxRetryAttempts":0,"retryDelaySeconds":1,"healthScenario":"Healthy","availabilityScenario":"Available"}""";
        var result = new IntegrationSettingsValidator().Validate(
            IntegrationType.Courier, IntegrationProviderCodes.Simulated, json, 1);

        Assert.False(result.IsValid);
        Assert.Contains("integrations.courier.invalidApiBaseUrl", result.ErrorCodes);
    }

    [Fact]
    public void CourierSecrets_AreMaskedRecursively()
    {
        const string json =
            """{"apiKey":"value-one","apiSecret":"value-two","accessToken":"value-three","webhookSecret":"value-four","accountId":"value-five","nested":{"password":"value-six"}}""";
        var masked = SensitiveConfigurationRedactor.MaskJson(json);

        foreach (var secret in new[] { "value-one", "value-two", "value-three", "value-four", "value-five", "value-six" })
        {
            Assert.DoesNotContain($"\"{secret}\"", masked, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CoverageSelection_PrefersGeographyThenService()
    {
        var integrationId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var countryId = Guid.NewGuid();
        var governorateId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var districtId = Guid.NewGuid();
        var location = new ValidatedCourierLocation(
            countryId, governorateId, cityId, districtId,
            [
                CourierScopeKeys.Geography(countryId),
                CourierScopeKeys.Geography(countryId, governorateId),
                CourierScopeKeys.Geography(countryId, governorateId, cityId),
                CourierScopeKeys.Geography(countryId, governorateId, cityId, districtId),
            ]);
        var countryServiceRule = NewRule(integrationId, serviceId, countryId, null, null, null);
        var cityGenericRule = NewRule(integrationId, null, countryId, governorateId, cityId, null);
        var districtGenericRule = NewRule(integrationId, null, countryId, governorateId, cityId, districtId);
        var districtServiceRule = NewRule(integrationId, serviceId, countryId, governorateId, cityId, districtId);

        var selected = CourierAvailabilityResolver.SelectCoverage(
            [countryServiceRule, cityGenericRule, districtGenericRule, districtServiceRule],
            serviceId,
            location);

        Assert.Same(districtServiceRule, selected);
    }

    [Fact]
    public void HealthFreshness_RejectsStaleFutureAndNonHealthyChecks()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.True(CourierHealthFreshnessPolicy.IsFreshHealthy(
            IntegrationHealthStatus.Healthy, now.AddMinutes(-59), now, 60));
        Assert.False(CourierHealthFreshnessPolicy.IsFreshHealthy(
            IntegrationHealthStatus.Healthy, now.AddMinutes(-61), now, 60));
        Assert.False(CourierHealthFreshnessPolicy.IsFreshHealthy(
            IntegrationHealthStatus.Healthy, now.AddSeconds(1), now, 60));
        Assert.False(CourierHealthFreshnessPolicy.IsFreshHealthy(
            IntegrationHealthStatus.Degraded, now, now, 60));
    }

    [Fact]
    public void HealthAlertPolicy_FiresOnceAtThresholdAndResetsAfterHealthy()
    {
        Assert.False(CourierHealthAlertPolicy.ShouldAlert(
            IntegrationHealthStatus.Unhealthy,
            [IntegrationHealthStatus.Degraded],
            3));
        Assert.True(CourierHealthAlertPolicy.ShouldAlert(
            IntegrationHealthStatus.Unhealthy,
            [IntegrationHealthStatus.Degraded, IntegrationHealthStatus.Unhealthy],
            3));
        Assert.False(CourierHealthAlertPolicy.ShouldAlert(
            IntegrationHealthStatus.Unhealthy,
            [IntegrationHealthStatus.Degraded, IntegrationHealthStatus.Unhealthy, IntegrationHealthStatus.Degraded],
            3));
        Assert.False(CourierHealthAlertPolicy.ShouldAlert(
            IntegrationHealthStatus.Unhealthy,
            [IntegrationHealthStatus.Healthy, IntegrationHealthStatus.Unhealthy],
            3));
        Assert.True(CourierHealthAlertPolicy.ShouldAlert(
            IntegrationHealthStatus.Degraded,
            [IntegrationHealthStatus.Unhealthy, IntegrationHealthStatus.Healthy],
            2));
        Assert.False(CourierHealthAlertPolicy.ShouldAlert(
            IntegrationHealthStatus.Healthy,
            [IntegrationHealthStatus.Unhealthy, IntegrationHealthStatus.Unhealthy],
            3));
    }

    [Fact]
    public void CourierFoundation_DoesNotDefineShipmentEntity()
    {
        Assert.Null(typeof(CourierService).Assembly.GetType("Schoolera.Domain.Entities.Shipment"));
        Assert.DoesNotContain(
            typeof(CourierAvailabilityOptionDto).GetProperties(),
            property => property.Name.Contains("Shipment", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CourierAdminWrites_RequireAntiforgery()
    {
        var writeActions = new[]
        {
            nameof(AdminCourierController.Profile),
            nameof(AdminCourierController.CreateService),
            nameof(AdminCourierController.UpdateService),
            nameof(AdminCourierController.ServiceActive),
            nameof(AdminCourierController.CreateCoverage),
            nameof(AdminCourierController.UpdateCoverage),
            nameof(AdminCourierController.CoverageActive),
            nameof(AdminCourierController.CreateWindow),
            nameof(AdminCourierController.UpdateWindow),
            nameof(AdminCourierController.WindowActive),
            nameof(AdminCourierController.CreateSla),
            nameof(AdminCourierController.UpdateSla),
            nameof(AdminCourierController.SlaActive),
        };

        foreach (var actionName in writeActions)
        {
            var action = typeof(AdminCourierController).GetMethod(actionName);
            Assert.NotNull(action);
            Assert.NotNull(action!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true)
                .SingleOrDefault());
        }
    }

    [Fact]
    public void CourierHealthCheck_UsesDedicatedRateLimit()
    {
        var action = typeof(AdminIntegrationsController)
            .GetMethod(nameof(AdminIntegrationsController.TestConnection));
        var rateLimit = Assert.Single(
            action!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>());

        Assert.Equal("courier-health-check", rateLimit.PolicyName);
    }

    [Fact]
    public async Task CourierSeeder_ProductionIsNoOpBeforeDatabaseAccess()
    {
        var options = new DbContextOptionsBuilder<SchooleraDbContext>()
            .UseSqlServer("Server=invalid.example;Database=never-contact;User Id=none;Password=none;TrustServerCertificate=True")
            .Options;
        await using var dbContext = new SchooleraDbContext(options);
        var seeder = new CourierSeeder(
            dbContext,
            new StubHostEnvironment(Environments.Production),
            NullLogger<CourierSeeder>.Instance);

        await seeder.SeedAsync();

        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task CourierSeeder_IsIdempotentAndNeverMutatesExistingCourierData()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        try
        {
            if (!await dbContext.Database.CanConnectAsync())
            {
                return;
            }
        }
        catch
        {
            return;
        }

        var before = await CourierSeedSnapshotAsync(dbContext);
        var seeder = new CourierSeeder(
            dbContext,
            new StubHostEnvironment(Environments.Development),
            NullLogger<CourierSeeder>.Instance);

        await seeder.SeedAsync();
        dbContext.ChangeTracker.Clear();
        var afterFirstRun = await CourierSeedSnapshotAsync(dbContext);
        await seeder.SeedAsync();
        dbContext.ChangeTracker.Clear();
        var afterSecondRun = await CourierSeedSnapshotAsync(dbContext);

        if (before.ConfigurationCount > 0)
        {
            Assert.Equal(before, afterFirstRun);
        }

        Assert.Equal(afterFirstRun, afterSecondRun);

        var fixture = await dbContext.PlatformIntegrationConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.IntegrationType == IntegrationType.Courier &&
                x.ProviderCode == IntegrationProviderCodes.Simulated &&
                x.DisplayNameEn == "Simulated Courier (Disabled)");
        if (fixture is not null)
        {
            var settings = IntegrationSettingsSerializer.Deserialize<CourierIntegrationSettings>(
                fixture.SettingsJson);
            Assert.False(fixture.IsActive);
            Assert.False(fixture.IsDefault);
            Assert.Null(settings.ApiKey);
            Assert.Null(settings.ApiSecret);
            Assert.Null(settings.AccessToken);
            Assert.Null(settings.WebhookSecret);
            Assert.Null(settings.AccountId);
        }
    }

    private static CourierProviderProfile NewProfile(string termsUrl, string privacyUrl) =>
        new(Guid.NewGuid(), "وصف", "Description", "/uploads/courier/logo.png", termsUrl, privacyUrl);

    private static CourierCoverageRule NewRule(
        Guid integrationId,
        Guid? serviceId,
        Guid countryId,
        Guid? governorateId,
        Guid? cityId,
        Guid? districtId) =>
        new(integrationId, serviceId, countryId, governorateId, cityId, districtId,
            CourierCoverageResult.Covered, null, null);

    private static async Task<CourierSeedSnapshot> CourierSeedSnapshotAsync(SchooleraDbContext dbContext)
    {
        var configurations = await dbContext.PlatformIntegrationConfigurations.AsNoTracking()
            .Where(x => x.IntegrationType == IntegrationType.Courier)
            .OrderBy(x => x.Id)
            .Select(x => $"{x.Id}|{x.ProviderCode}|{x.DisplayNameAr}|{x.DisplayNameEn}|{x.SettingsJson}|{x.IsActive}|{x.IsDefault}|{x.SortOrder}")
            .ToArrayAsync();
        return new CourierSeedSnapshot(
            configurations.Length,
            string.Join("\n", configurations),
            await dbContext.CourierProviderProfiles.CountAsync(),
            await dbContext.CourierServices.CountAsync(),
            await dbContext.CourierCoverageRules.CountAsync(),
            await dbContext.CourierOperatingWindows.CountAsync(),
            await dbContext.CourierSlaDefinitions.CountAsync());
    }

    private sealed record CourierSeedSnapshot(
        int ConfigurationCount,
        string Configurations,
        int ProfileCount,
        int ServiceCount,
        int CoverageCount,
        int WindowCount,
        int SlaCount);

    private static UpsertCourierServiceRequest NewServiceRequest(
        bool supportsDropOffPoint, string expectedRowVersion) =>
        new("HOME", "استلام منزلي", "Home pickup", "وصف", "Description",
            SortOrder: 1, MinimumPickupLeadTimeMinutes: 30, DailyCutoffLocalTime: new TimeOnly(14, 0),
            MaximumFuturePickupDays: 7, AcceptanceWindowMinutes: 60,
            SupportsScheduledPickup: true, SupportsSameDayPickup: true,
            CanCreatePickup: false, CanQueryStatus: false, SupportsWebhook: false,
            SupportsPolling: false, SupportsManualUpdates: false,
            SupportsCancellationBeforePickup: true, SupportsCourierAssignment: false,
            SupportsProofOfPickup: false, SupportsProofOfDelivery: false,
            SupportsDropOffPoint: supportsDropOffPoint, MaximumEnvelopeWeightGrams: 1000,
            MaximumEnvelopeLengthCm: 30, MaximumEnvelopeWidthCm: 20,
            MaximumEnvelopeHeightCm: 5, ExpectedRowVersion: expectedRowVersion,
            ExpectedConfigurationVersion: 1);

    private sealed class ParentCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => role == SchooleraRoles.Parent;
    }

    private sealed class MissingDestinationReader : ICourierAdmissionContextReader
    {
        public Guid? RequestedParentUserId { get; private set; }
        public Task<CourierAdmissionDestination?> GetOwnedDestinationAsync(
            Guid parentUserId, Guid applicationId, CancellationToken cancellationToken = default)
        {
            RequestedParentUserId = parentUserId;
            return Task.FromResult<CourierAdmissionDestination?>(null);
        }
    }

    private sealed class ThrowingAvailabilityResolver : ICourierAvailabilityResolver
    {
        public bool WasCalled { get; private set; }
        public Task<Result<IReadOnlyList<CourierAvailabilityOptionDto>>> ResolveAsync(
            CourierAvailabilityRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            throw new InvalidOperationException("Resolver must not be called for a non-owned application.");
        }
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Schoolera.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
