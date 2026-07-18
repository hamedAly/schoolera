using Schoolera.Application.Integrations.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Application.Meetings;

namespace Schoolera.Application.Integrations.Common;

public static class IntegrationMapping
{
    public static PlatformIntegrationSummaryDto ToSummary(PlatformIntegrationConfiguration entity) =>
        new(
            entity.Id,
            entity.IntegrationType,
            entity.ProviderCode,
            entity.DisplayNameAr,
            entity.DisplayNameEn,
            entity.SettingsSchemaVersion,
            entity.IsActive,
            entity.IsDefault,
            entity.HealthStatus,
            entity.LastHealthCheckAtUtc,
            entity.LastSuccessfulUseAtUtc,
            entity.LastSafeFailureCode,
            entity.SortOrder,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.RowVersion,
            ToMeetingMetadata(entity));

    public static PlatformIntegrationDetailDto ToDetail(PlatformIntegrationConfiguration entity) =>
        new(
            entity.Id,
            entity.IntegrationType,
            entity.ProviderCode,
            entity.DisplayNameAr,
            entity.DisplayNameEn,
            SensitiveConfigurationRedactor.MaskJson(entity.SettingsJson),
            entity.SettingsSchemaVersion,
            entity.IsActive,
            entity.IsDefault,
            entity.HealthStatus,
            entity.LastHealthCheckAtUtc,
            entity.LastSuccessfulUseAtUtc,
            entity.LastSafeFailureCode,
            entity.SortOrder,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.RowVersion,
            ToMeetingMetadata(entity));

    private static SafeMeetingProviderMetadataDto? ToMeetingMetadata(
        PlatformIntegrationConfiguration entity)
    {
        if (entity.IntegrationType != IntegrationType.Meeting) return null;
        try
        {
            var settings = IntegrationSettingsSerializer.Deserialize<MeetingIntegrationSettings>(
                entity.SettingsJson);
            return new(
                entity.ProviderCode,
                settings.PublicNameAr,
                settings.PublicNameEn,
                settings.Environment,
                entity.IsActive,
                entity.HealthStatus,
                settings.SupportsMeetingCreation,
                settings.SupportsParentAccess,
                settings.SupportsHostAccess,
                settings.SupportsCancellation,
                settings.SupportsStatusQuery,
                settings.TermsUrl,
                settings.PrivacyUrl,
                IntegrationProviderCodes.IsSimulated(entity.ProviderCode));
        }
        catch
        {
            return null;
        }
    }

    public static NotificationTemplateListItemDto ToTemplateListItem(NotificationTemplate template)
    {
        var versions = template.Versions;
        var latest = versions.Count == 0 ? 0 : versions.Max(v => v.VersionNumber);
        var published = versions
            .Where(v => v.IsPublished)
            .Select(v => (int?)v.VersionNumber)
            .DefaultIfEmpty(null)
            .Max();

        return new NotificationTemplateListItemDto(
            template.Id,
            template.EventType,
            template.Channel,
            template.Culture,
            template.Code,
            template.IsActive,
            latest,
            published,
            template.UpdatedAtUtc);
    }

    public static NotificationTemplateVersionDto ToTemplateVersion(
        NotificationTemplateVersion version,
        NotificationTemplate? template = null)
    {
        var header = template ?? version.Template;
        return new NotificationTemplateVersionDto(
            version.Id,
            version.TemplateId,
            header.EventType,
            header.Channel,
            header.Culture,
            header.Code,
            version.VersionNumber,
            version.Subject,
            version.Body,
            version.AllowedVariablesCsv,
            version.ProviderTemplateId,
            version.IsPublished,
            version.PublishedAtUtc,
            version.CreatedByUserId,
            version.CreatedAtUtc);
    }

    public static bool HasRowVersionMismatch(byte[]? requested, byte[] current) =>
        requested is { Length: > 0 } &&
        (current.Length == 0 || !requested.SequenceEqual(current));
}
