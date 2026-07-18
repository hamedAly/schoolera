using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Notifications;

public sealed class PlatformIntegrationConfigurationAccessor(
    SchooleraDbContext dbContext,
    IMemoryCache memoryCache,
    IIntegrationSettingsValidator settingsValidator) : IPlatformIntegrationConfigurationAccessor
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public async Task<ResolvedIntegrationConfiguration?> GetActiveDefaultAsync(
        IntegrationType integrationType,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(integrationType);
        if (memoryCache.TryGetValue(cacheKey, out ResolvedIntegrationConfiguration? cached))
        {
            return cached;
        }

        var entity = await dbContext.PlatformIntegrationConfigurations
            .AsNoTracking()
            .Where(item =>
                item.IntegrationType == integrationType &&
                item.IsActive &&
                item.IsDefault)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        ResolvedIntegrationConfiguration? resolved = null;
        if (entity is not null)
        {
            var validation = settingsValidator.Validate(
                entity.IntegrationType,
                entity.ProviderCode,
                entity.SettingsJson,
                entity.SettingsSchemaVersion);

            resolved = new ResolvedIntegrationConfiguration(
                entity.Id,
                entity.IntegrationType,
                entity.ProviderCode,
                entity.SettingsJson,
                entity.SettingsSchemaVersion,
                IsConfigured: validation.IsValid);
        }

        memoryCache.Set(cacheKey, resolved, CacheTtl);
        return resolved;
    }

    public void InvalidateCache(IntegrationType? integrationType = null)
    {
        if (integrationType is null)
        {
            foreach (IntegrationType type in Enum.GetValues<IntegrationType>())
            {
                memoryCache.Remove(CacheKey(type));
            }

            return;
        }

        memoryCache.Remove(CacheKey(integrationType.Value));
    }

    private static string CacheKey(IntegrationType integrationType) =>
        $"platform-integration:default:{(int)integrationType}";
}
