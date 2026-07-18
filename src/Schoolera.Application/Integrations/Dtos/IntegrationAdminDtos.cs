using Schoolera.Domain.Enums;
using Schoolera.Application.Meetings;

namespace Schoolera.Application.Integrations.Dtos;

public sealed record PlatformIntegrationSummaryDto(
    Guid Id,
    IntegrationType IntegrationType,
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn,
    int SettingsSchemaVersion,
    bool IsActive,
    bool IsDefault,
    IntegrationHealthStatus HealthStatus,
    DateTimeOffset? LastHealthCheckAtUtc,
    DateTimeOffset? LastSuccessfulUseAtUtc,
    string? LastSafeFailureCode,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion,
    SafeMeetingProviderMetadataDto? MeetingMetadata);

public sealed record PlatformIntegrationDetailDto(
    Guid Id,
    IntegrationType IntegrationType,
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn,
    string MaskedSettingsJson,
    int SettingsSchemaVersion,
    bool IsActive,
    bool IsDefault,
    IntegrationHealthStatus HealthStatus,
    DateTimeOffset? LastHealthCheckAtUtc,
    DateTimeOffset? LastSuccessfulUseAtUtc,
    string? LastSafeFailureCode,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion,
    SafeMeetingProviderMetadataDto? MeetingMetadata);

public sealed record CreatePlatformIntegrationRequest(
    IntegrationType IntegrationType,
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn,
    string SettingsJson,
    int SettingsSchemaVersion = 1,
    int SortOrder = 0);

public sealed record UpdatePlatformIntegrationRequest(
    string ProviderCode,
    string DisplayNameAr,
    string? DisplayNameEn,
    string SettingsJson,
    int SettingsSchemaVersion,
    int SortOrder,
    IReadOnlyList<string>? ClearSensitiveProperties,
    byte[]? RowVersion);

public sealed record ValidateIntegrationRequest(
    IntegrationType? IntegrationType,
    string? ProviderCode,
    string SettingsJson,
    int? SettingsSchemaVersion);

public sealed record TestConnectionResultDto(
    bool Succeeded,
    string ProviderCode,
    IntegrationHealthStatus HealthStatus,
    string? SafeMessageCode);

public sealed record NotificationTemplateListItemDto(
    Guid Id,
    NotificationEventType EventType,
    NotificationChannel Channel,
    string Culture,
    string Code,
    bool IsActive,
    int LatestVersionNumber,
    int? PublishedVersionNumber,
    DateTimeOffset UpdatedAtUtc);

public sealed record NotificationTemplateVersionDto(
    Guid Id,
    Guid TemplateId,
    NotificationEventType EventType,
    NotificationChannel Channel,
    string Culture,
    string TemplateCode,
    int VersionNumber,
    string? Subject,
    string Body,
    string AllowedVariablesCsv,
    string? ProviderTemplateId,
    bool IsPublished,
    DateTimeOffset? PublishedAtUtc,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateTemplateVersionRequest(
    NotificationEventType EventType,
    NotificationChannel Channel,
    string Culture,
    string? Code,
    string? Subject,
    string Body,
    string AllowedVariablesCsv,
    string? ProviderTemplateId);

public sealed record NotificationOpsSummaryDto(
    int Pending,
    int Failed,
    int DeadLetter,
    int Processing);
