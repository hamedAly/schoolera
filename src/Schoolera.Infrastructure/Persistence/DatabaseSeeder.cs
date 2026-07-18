using Microsoft.Extensions.Logging;
using Schoolera.Infrastructure.Notifications;
using Schoolera.Infrastructure.Payments;

namespace Schoolera.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    SchoolCatalogSeeder schoolCatalogSeeder,
    OnboardingSeeder onboardingSeeder,
    SchoolPortalSeeder schoolPortalSeeder,
    CmsContentSeeder cmsContentSeeder,
    NotificationSeeder notificationSeeder,
    FavoritesSeeder favoritesSeeder,
    SupportTicketsSeeder supportTicketsSeeder,
    PaymentSeeder paymentSeeder,
    CourierSeeder courierSeeder,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running database seed...");
        await schoolCatalogSeeder.SeedAsync(cancellationToken);
        await onboardingSeeder.SeedAsync(cancellationToken);
        await schoolPortalSeeder.SeedAsync(cancellationToken);
        await cmsContentSeeder.SeedAsync(cancellationToken);
        await notificationSeeder.SeedAsync(cancellationToken);
        await favoritesSeeder.SeedAsync(cancellationToken);
        await supportTicketsSeeder.SeedAsync(cancellationToken);
        await paymentSeeder.SeedAsync(cancellationToken);
        await courierSeeder.SeedAsync(cancellationToken);
        logger.LogInformation("Database seed completed.");
    }
}
