using System.Text.Json;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Platform-owned external integration configuration. Provider credentials live in SettingsJson (Phase 1 plaintext).
/// </summary>
public sealed class PlatformIntegrationConfiguration
{
    private PlatformIntegrationConfiguration()
    {
    }

    public PlatformIntegrationConfiguration(
        IntegrationType integrationType,
        string providerCode,
        string displayNameAr,
        string? displayNameEn,
        string settingsJson,
        int settingsSchemaVersion,
        int sortOrder = 0)
    {
        if (!Enum.IsDefined(integrationType))
        {
            throw new ArgumentOutOfRangeException(nameof(integrationType));
        }

        Id = Guid.NewGuid();
        IntegrationType = integrationType;
        ProviderCode = NormalizeProvider(providerCode);
        DisplayNameAr = displayNameAr.Trim();
        DisplayNameEn = string.IsNullOrWhiteSpace(displayNameEn) ? null : displayNameEn.Trim();
        SettingsJson = ValidateJson(settingsJson);
        SettingsSchemaVersion = settingsSchemaVersion < 1
            ? throw new ArgumentOutOfRangeException(nameof(settingsSchemaVersion))
            : settingsSchemaVersion;
        IsActive = false;
        IsDefault = false;
        HealthStatus = IntegrationHealthStatus.Unknown;
        SortOrder = sortOrder;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public IntegrationType IntegrationType { get; private set; }

    public string ProviderCode { get; private set; } = string.Empty;

    public string DisplayNameAr { get; private set; } = string.Empty;

    public string? DisplayNameEn { get; private set; }

    /// <summary>Phase 1 plaintext JSON. Do not log or audit full content.</summary>
    public string SettingsJson { get; private set; } = "{}";

    public int SettingsSchemaVersion { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDefault { get; private set; }

    public IntegrationHealthStatus HealthStatus { get; private set; }

    public DateTimeOffset? LastHealthCheckAtUtc { get; private set; }

    public DateTimeOffset? LastSuccessfulUseAtUtc { get; private set; }

    public string? LastSafeFailureCode { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void UpdateDetails(
        string providerCode,
        string displayNameAr,
        string? displayNameEn,
        string settingsJson,
        int settingsSchemaVersion,
        int sortOrder)
    {
        ProviderCode = NormalizeProvider(providerCode);
        DisplayNameAr = displayNameAr.Trim();
        DisplayNameEn = string.IsNullOrWhiteSpace(displayNameEn) ? null : displayNameEn.Trim();
        SettingsJson = ValidateJson(settingsJson);
        SettingsSchemaVersion = settingsSchemaVersion < 1
            ? throw new ArgumentOutOfRangeException(nameof(settingsSchemaVersion))
            : settingsSchemaVersion;
        SortOrder = sortOrder;
        Touch();
    }

    public void ReplaceSettingsJson(string settingsJson)
    {
        SettingsJson = ValidateJson(settingsJson);
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
        Touch();
    }

    public void SetAsDefault()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Only an active configuration can be the default.");
        }

        IsDefault = true;
        Touch();
    }

    public void ClearDefault()
    {
        IsDefault = false;
        Touch();
    }

    public void RecordHealth(IntegrationHealthStatus status, string? safeFailureCode)
    {
        HealthStatus = status;
        LastHealthCheckAtUtc = DateTimeOffset.UtcNow;
        LastSafeFailureCode = string.IsNullOrWhiteSpace(safeFailureCode)
            ? null
            : safeFailureCode.Trim()[..Math.Min(safeFailureCode.Trim().Length, 100)];
        Touch();
    }

    public void RecordSuccessfulUse()
    {
        LastSuccessfulUseAtUtc = DateTimeOffset.UtcNow;
        HealthStatus = IntegrationHealthStatus.Healthy;
        LastSafeFailureCode = null;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string NormalizeProvider(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            throw new ArgumentException("Provider code is required.", nameof(providerCode));
        }

        return providerCode.Trim();
    }

    private static string ValidateJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            throw new ArgumentException("Settings JSON is required.", nameof(settingsJson));
        }

        try
        {
            using var document = JsonDocument.Parse(settingsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Settings JSON must be an object.", nameof(settingsJson));
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Settings JSON is malformed.", nameof(settingsJson), ex);
        }

        if (settingsJson.Length > 16_000)
        {
            throw new ArgumentException("Settings JSON exceeds maximum length.", nameof(settingsJson));
        }

        return settingsJson;
    }
}
