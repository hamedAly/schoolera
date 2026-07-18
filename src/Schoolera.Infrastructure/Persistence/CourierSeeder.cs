using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Data;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence;

/// <summary>
/// Idempotent simulated courier configuration seed. Existing courier configuration is never changed.
/// The fixture contains no credentials, provider payloads, or personal information.
/// </summary>
public sealed class CourierSeeder(
    SchooleraDbContext dbContext,
    IHostEnvironment environment,
    ILogger<CourierSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Test"))
        {
            return;
        }

        logger.LogInformation("Running simulated courier seed...");
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000",
                ["Schoolera:CourierSeeder"],
                cancellationToken);

            if (await dbContext.PlatformIntegrationConfigurations
                .AsNoTracking()
                .AnyAsync(integration => integration.IntegrationType == IntegrationType.Courier, cancellationToken))
            {
                logger.LogInformation("Courier seed skipped because courier configuration already exists.");
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var egypt = await dbContext.Countries
                .AsNoTracking()
                .SingleOrDefaultAsync(country => country.Code == "EG", cancellationToken);
            var cairoGovernorate = egypt is null
                ? null
                : await dbContext.Governorates
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        governorate => governorate.CountryId == egypt.Id && governorate.Slug == "cairo",
                        cancellationToken);
            var cairoCity = cairoGovernorate is null
                ? null
                : await dbContext.Cities
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        city => city.GovernorateId == cairoGovernorate.Id && city.Slug == "cairo",
                        cancellationToken);

            if (egypt is null || cairoGovernorate is null || cairoCity is null)
            {
                logger.LogWarning("Courier seed skipped because Egypt/Cairo taxonomy is unavailable.");
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var integration = new PlatformIntegrationConfiguration(
                IntegrationType.Courier,
                IntegrationProviderCodes.Simulated,
                "مندوب محاكى (معطل)",
                "Simulated Courier (Disabled)",
                JsonSerializer.Serialize(new CourierIntegrationSettings
                {
                    Environment = nameof(CourierProviderEnvironment.Simulated),
                    RequestTimeoutSeconds = 10,
                    MaxRetryAttempts = 0,
                    RetryDelaySeconds = 1,
                    HealthScenario = nameof(SimulatedCourierHealthScenario.Healthy),
                    AvailabilityScenario = nameof(SimulatedCourierAvailabilityScenario.Available),
                }),
                settingsSchemaVersion: 1,
                sortOrder: 30);

            var profile = new CourierProviderProfile(
                integration.Id,
                "ملف تجريبي لخدمة استلام المظاريف من المنزل. غير مخصص للاستخدام الفعلي.",
                "Development profile for home envelope pickup. Not for real-world use.",
                logoReference: null,
                termsUrl: "/terms",
                privacyUrl: "/privacy");

            var service = new CourierService(
                integration.Id,
                code: "HOME-PICKUP",
                nameAr: "استلام من المنزل (تجريبي)",
                nameEn: "Home Pickup (Simulated)",
                descriptionAr: "استلام مظروف مستندات تجريبي من عنوان العميل.",
                descriptionEn: "Simulated document-envelope pickup from the customer's address.",
                sortOrder: 1,
                minimumPickupLeadTimeMinutes: 120,
                dailyCutoffLocalTime: new TimeOnly(15, 0),
                maximumFuturePickupDays: 14,
                acceptanceWindowMinutes: 30,
                supportsScheduledPickup: true,
                supportsSameDayPickup: false,
                canCreatePickup: false,
                canQueryStatus: true,
                supportsWebhook: false,
                supportsPolling: true,
                supportsManualUpdates: true,
                supportsCancellationBeforePickup: true,
                supportsCourierAssignment: false,
                supportsProofOfPickup: false,
                supportsProofOfDelivery: false,
                supportsDropOffPoint: false,
                maximumEnvelopeWeightGrams: 1000,
                maximumEnvelopeLengthCm: 35m,
                maximumEnvelopeWidthCm: 25m,
                maximumEnvelopeHeightCm: 5m);

            var egyptCoverage = new CourierCoverageRule(
                integration.Id,
                service.Id,
                egypt.Id,
                governorateId: null,
                cityId: null,
                districtId: null,
                CourierCoverageResult.Covered,
                notesAr: "بيانات تطوير فقط؛ يظل التكامل والخدمة معطلين.",
                notesEn: "Development fixture; integration and service remain disabled.");

            dbContext.PlatformIntegrationConfigurations.Add(integration);
            dbContext.CourierProviderProfiles.Add(profile);
            dbContext.CourierServices.Add(service);
            dbContext.CourierCoverageRules.Add(egyptCoverage);

            var cairoScope = CourierScopeKeys.Geography(
                egypt.Id,
                cairoGovernorate.Id,
                cairoCity.Id);
            foreach (var day in new[]
                 {
                     DayOfWeek.Sunday,
                     DayOfWeek.Monday,
                     DayOfWeek.Tuesday,
                     DayOfWeek.Wednesday,
                     DayOfWeek.Thursday,
                 })
            {
                dbContext.CourierOperatingWindows.Add(new CourierOperatingWindow(
                    integration.Id,
                    service.Id,
                    coverageRuleId: null,
                    cairoScope,
                    timeZoneId: "Africa/Cairo",
                    day,
                    localStartTime: new TimeOnly(9, 0),
                    localEndTime: new TimeOnly(17, 0)));
            }

            dbContext.CourierSlaDefinitions.Add(new CourierSlaDefinition(
                integration.Id,
                service.Id,
                coverageRuleId: null,
                cairoScope,
                acceptanceTargetMinutes: 30,
                pickupSchedulingTargetMinutes: 120,
                pickupCompletionTargetMinutes: 240,
                deliveryToSchoolTargetMinutes: 2880,
                receiptConfirmationTargetMinutes: null));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
        logger.LogInformation("Disabled simulated courier configuration seeded.");
    }
}
