using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Options;
using Schoolera.Infrastructure.SchoolPortal;
using Schoolera.Infrastructure.Admissions;
using Schoolera.Infrastructure.Analytics;
using Schoolera.Infrastructure.Identity;
using Schoolera.Infrastructure.Parent;
using Schoolera.Infrastructure.Persistence;
using Schoolera.Infrastructure.Persistence.Repositories;
using Schoolera.Infrastructure.Storage;
using Schoolera.Infrastructure.Notifications;
using Schoolera.Infrastructure.Meetings;
using Schoolera.Application.Meetings;
using Schoolera.Application.Parent.Options;
using Schoolera.Application.SupportTickets.Options;
using Schoolera.Infrastructure.SupportTickets;
using Schoolera.Infrastructure.Payments;
using Schoolera.Infrastructure.Couriers;
using Schoolera.Application.Couriers;
using Microsoft.AspNetCore.DataProtection;

namespace Schoolera.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<SchoolPortalMediaOptions>(configuration.GetSection(SchoolPortalMediaOptions.SectionName));
        services.Configure<ParentChildOptions>(configuration.GetSection(ParentChildOptions.SectionName));
        services.Configure<ParentIdentityProtectionOptions>(
            configuration.GetSection(ParentIdentityProtectionOptions.SectionName));
        services.AddOptions<CourierOptions>()
            .Bind(configuration.GetSection(CourierOptions.SectionName))
            .Validate(
                options => options.HealthFreshnessMinutes is >= 1 and <= 1440 &&
                           options.HealthFailureAlertThreshold is >= 1 and <= 100,
                "Courier health freshness must be 1-1440 minutes and alert threshold must be 1-100.")
            .ValidateOnStart();
        // Development / non-production SLA foundation — not approved production values.
        services.Configure<SupportTicketSlaOptions>(
            configuration.GetSection(SupportTicketSlaOptions.SectionName));

        services.AddDataProtection();
        services.AddSingleton<IChildIdentityProtector, ChildIdentityProtector>();
        services.AddScoped<IParentAccountService, ParentAccountService>();
        services.AddScoped<IParentProfileRepository, ParentProfileRepository>();
        services.AddScoped<IChildProfileRepository, ChildProfileRepository>();
        services.AddScoped<IChildDocumentRepository, ChildDocumentRepository>();
        services.AddScoped<ILegalConsentService, Legal.LegalConsentService>();

        services.AddSingleton<IValidateOptions<FileStorageOptions>, FileStorageOptionsValidator>();
        services
            .AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .Configure<IHostEnvironment>((options, environment) =>
            {
                if (!Path.IsPathRooted(options.StorageRoot))
                {
                    options.StorageRoot = Path.GetFullPath(
                        Path.Combine(environment.ContentRootPath, options.StorageRoot));
                }
                else
                {
                    options.StorageRoot = Path.GetFullPath(options.StorageRoot);
                }

                options.PublicRequestPath = string.IsNullOrWhiteSpace(options.PublicRequestPath)
                    ? "/uploads"
                    : "/" + options.PublicRequestPath.Trim().Trim('/');

                options.AllowedExtensions = options.AllowedExtensions
                    .Select(extension => extension.StartsWith(".", StringComparison.Ordinal)
                        ? extension.ToLowerInvariant()
                        : $".{extension.ToLowerInvariant()}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                options.AllowedContentTypes = options.AllowedContentTypes
                    .Select(contentType => contentType.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            })
            .ValidateOnStart();

        services.AddDbContext<SchooleraDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        services
            .AddOptions<PrivateFileStorageOptions>()
            .Bind(configuration.GetSection(PrivateFileStorageOptions.SectionName))
            .Configure<IHostEnvironment>((privateOptions, hostEnvironment) =>
            {
                privateOptions.StorageRoot = Path.IsPathRooted(privateOptions.StorageRoot)
                    ? Path.GetFullPath(privateOptions.StorageRoot)
                    : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, privateOptions.StorageRoot));

                privateOptions.AllowedExtensions = privateOptions.AllowedExtensions
                    .Select(extension => extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                privateOptions.AllowedContentTypes = privateOptions.AllowedContentTypes
                    .Select(contentType => contentType.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            });

        services.AddHttpContextAccessor();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddScoped<SchoolReadRepository>();
        services.AddScoped<ISchoolReadRepository>(sp => sp.GetRequiredService<SchoolReadRepository>());
        services.AddScoped<ISchoolMapPinReadRepository>(sp => sp.GetRequiredService<SchoolReadRepository>());
        services.AddScoped<ISchoolContactLeadRepository, SchoolContactLeadRepository>();
        services.AddSingleton<SchoolProfileViewQueue>();
        services.AddSingleton<ISchoolProfileViewQueue>(sp => sp.GetRequiredService<SchoolProfileViewQueue>());
        services.AddHostedService<SchoolProfileViewBackgroundService>();
        services.AddScoped<ITaxonomyRepository, TaxonomyRepository>();
        services.AddScoped<ISchoolOnboardingRepository, SchoolOnboardingRepository>();
        services.AddScoped<ISchoolPortalRepository, SchoolPortalRepository>();
        services.AddScoped<ISchoolPortalAccess, SchoolPortalAccess>();
        services.AddScoped<ISchoolPortalAuditWriter, SchoolPortalAuditWriter>();
        services.AddScoped<IAdminPlatformService, Admin.AdminPlatformService>();
        services.AddScoped<IAdmissionApplicationRepository, AdmissionApplicationRepository>();
        services.AddScoped<IAdmissionApplicationNumberGenerator, AdmissionApplicationNumberGenerator>();
        services.AddScoped<ISchoolAdmissionRequirementRepository, SchoolAdmissionRequirementRepository>();
        services.AddScoped<IAdmissionRequirementSnapshotService, Application.Admissions.Common.AdmissionRequirementSnapshotService>();
        services.AddScoped<IAdmissionRequirementCompletenessService, Application.Admissions.Common.AdmissionRequirementCompletenessService>();
        services.AddScoped<ISchoolInterviewAssessmentPolicyRepository, SchoolInterviewAssessmentPolicyRepository>();
        services.AddScoped<IInterviewAssessmentSlotRepository, InterviewAssessmentSlotRepository>();
        services.AddScoped<IAdmissionEvaluationRepository, AdmissionEvaluationRepository>();
        services.AddScoped<IAdmissionInterviewAssessmentPolicySnapshotService, Application.Admissions.Common.AdmissionInterviewAssessmentPolicySnapshotService>();
        services.AddScoped<ISchoolChildAgeEligibilityRuleRepository, SchoolChildAgeEligibilityRuleRepository>();
        services.AddScoped<IAdmissionChildAgeEligibilitySnapshotService, Application.Admissions.Common.AdmissionChildAgeEligibilitySnapshotService>();
        services.AddScoped<ISchoolAdmissionQuestionRepository, SchoolAdmissionQuestionRepository>();
        services.AddScoped<IAdmissionQuestionSnapshotService, Application.Admissions.Common.AdmissionQuestionSnapshotService>();
        services.AddScoped<IAdmissionQuestionCompletenessService, Application.Admissions.Common.AdmissionQuestionCompletenessService>();
        services.AddScoped<ICmsRepository, CmsRepository>();
        services.AddScoped<IFavoriteSchoolRepository, FavoriteSchoolRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ISupportTicketReferenceGenerator, SupportTicketReferenceGenerator>();
        services.AddSingleton<IContentSanitizer, Cms.ContentSanitizer>();
        services.AddScoped<SchoolCatalogSeeder>();
        services.AddScoped<OnboardingSeeder>();
        services.AddScoped<SchoolPortalSeeder>();
        services.AddScoped<AdmissionApplicationSeeder>();
        services.AddScoped<CmsContentSeeder>();
        services.AddScoped<NotificationSeeder>();
        services.AddScoped<FavoritesSeeder>();
        services.AddScoped<SupportTicketsSeeder>();
        services.AddScoped<PaymentSeeder>();
        services.AddScoped<CourierSeeder>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IPrivateFileStorage, LocalPrivateFileStorage>();
        services.AddScoped<ISchoolPortalMediaCleanup, SchoolPortalMediaOrphanCleanup>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IUserDirectory, IdentityUserDirectory>();

        services.AddMemoryCache();
        services
            .AddOptions<NotificationWorkerOptions>()
            .Bind(configuration.GetSection(NotificationWorkerOptions.SectionName));
        services.AddScoped<IPlatformIntegrationConfigurationAccessor, PlatformIntegrationConfigurationAccessor>();
        services.AddScoped<INotificationTemplateRenderer, NotificationTemplateRenderer>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationOutboxPublisher, NotificationOutboxPublisher>();
        services.AddScoped<SimulatedEmailNotificationProvider>();
        services.AddScoped<SmtpEmailNotificationProvider>();
        services.AddScoped<INotificationChannelProvider, InAppNotificationProvider>();
        services.AddScoped<INotificationChannelProvider, NotificationChannelProviderRouter>();
        services.AddScoped<INotificationChannelProvider, SimulatedSmsNotificationProvider>();
        services.AddScoped<INotificationChannelProvider, SimulatedWhatsAppNotificationProvider>();
        services.AddHostedService<NotificationOutboxWorker>();
        services.AddSingleton<IRuntimeEnvironment, RuntimeEnvironment>();
        services.AddScoped<ICourierConfigurationResolver, CourierConfigurationResolver>();
        services.AddScoped<ICourierLocationHierarchyValidator, CourierLocationHierarchyValidator>();
        services.AddScoped<ICourierAvailabilityResolver, CourierAvailabilityResolver>();
        services.AddScoped<ICourierHealthService, CourierHealthService>();
        services.AddScoped<ICourierAdministrationService, CourierAdministrationService>();
        services.AddScoped<ICourierAdmissionContextReader, CourierAdmissionContextReader>();
        if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
        {
            services.AddScoped<ICourierProvider, SimulatedCourierProvider>();
        }
        services.AddScoped<IMeetingSessionService, MeetingSessionService>();
        if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
        {
            services.AddScoped<IMeetingProvider, SimulatedMeetingProvider>();
        }
        services
            .AddOptions<MeetingSessionWorkerOptions>()
            .Bind(configuration.GetSection(MeetingSessionWorkerOptions.SectionName));
        services.AddHostedService<MeetingSessionWorker>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentReferenceGenerator, PaymentReferenceGenerator>();

        services.AddSchooleraEmail(configuration, environment);
        services.AddSchooleraIdentity(configuration, environment);

        return services;
    }

    /// <summary>
    /// Optionally applies EF Core migrations and seeds data based on <c>Database</c> appsettings.
    /// Migration failures stop startup; seed never runs after a failed migration.
    /// </summary>
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        if (options.ApplyMigrations)
        {
            try
            {
                logger.LogInformation("Database migrations enabled. Applying pending EF Core migrations...");
                var dbContext = provider.GetRequiredService<SchooleraDbContext>();
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations completed successfully.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Database migrations failed. Application startup will stop.");
                throw;
            }
        }
        else
        {
            logger.LogInformation("Database migrations disabled (Database:ApplyMigrations=false).");
        }

        if (!options.SeedData)
        {
            logger.LogInformation("Database seed disabled (Database:SeedData=false).");
            return;
        }

        try
        {
            var seeder = provider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(cancellationToken);

            var authSeeder = provider.GetRequiredService<AuthDataSeeder>();
            await authSeeder.SeedAsync(cancellationToken);

            var portalSeeder = provider.GetRequiredService<SchoolPortalSeeder>();
            await portalSeeder.SeedAsync(cancellationToken);

            var admissionSeeder = provider.GetRequiredService<AdmissionApplicationSeeder>();
            await admissionSeeder.SeedAsync(cancellationToken);

            var favoritesSeeder = provider.GetRequiredService<FavoritesSeeder>();
            await favoritesSeeder.SeedAsync(cancellationToken);

            var supportTicketsSeeder = provider.GetRequiredService<SupportTicketsSeeder>();
            await supportTicketsSeeder.SeedAsync(cancellationToken);

            var paymentSeeder = provider.GetRequiredService<PaymentSeeder>();
            await paymentSeeder.SeedAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database seed failed.");
            throw;
        }
    }
}
