using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Integrations;

/// <summary>Centralized redaction for known sensitive JSON property names (case-insensitive).</summary>
public static class SensitiveConfigurationRedactor
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApiKey",
        "ApiSecret",
        "AccessToken",
        "AuthToken",
        "Password",
        "WebhookSecret",
        "WebhookVerifyToken",
        "ClientSecret",
        "PrivateKey",
        "ConnectionString",
        "ServerApiKey",
        "ServerSideApiKey",
        "MerchantId",
        "AccountId",
        "MerchantSecret",
    };

    public const string MaskToken = "***";

    public static bool IsSensitiveName(string propertyName) =>
        SensitiveNames.Contains(propertyName);

    public static string MaskJson(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(settingsJson);
            var masked = MaskElement(document.RootElement);
            return JsonSerializer.Serialize(masked);
        }
        catch (JsonException)
        {
            return "{\"error\":\"invalid_json\"}";
        }
    }

    /// <summary>
    /// Merges submitted JSON with stored JSON. Masked unchanged values preserve stored secrets.
    /// Explicit null or empty string with clearSensitive=true clears a property.
    /// </summary>
    public static string MergePreservingSecrets(
        string storedJson,
        string submittedJson,
        IReadOnlySet<string>? clearSensitiveProperties = null)
    {
        using var stored = JsonDocument.Parse(string.IsNullOrWhiteSpace(storedJson) ? "{}" : storedJson);
        using var submitted = JsonDocument.Parse(string.IsNullOrWhiteSpace(submittedJson) ? "{}" : submittedJson);

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in stored.RootElement.EnumerateObject())
        {
            result[property.Name] = ToObject(property.Value);
        }

        foreach (var property in submitted.RootElement.EnumerateObject())
        {
            if (clearSensitiveProperties?.Contains(property.Name) == true && IsSensitiveName(property.Name))
            {
                result.Remove(property.Name);
                continue;
            }

            if (IsSensitiveName(property.Name) &&
                property.Value.ValueKind == JsonValueKind.String &&
                property.Value.GetString() == MaskToken)
            {
                continue;
            }

            result[property.Name] = ToObject(property.Value);
        }

        return JsonSerializer.Serialize(result);
    }

    private static object? ToObject(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ToObject(p.Value), StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Select(ToObject).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };

    public static string RedactForLog(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var result = text;
        foreach (var name in SensitiveNames)
        {
            result = Regex.Replace(
                result,
                $@"""{Regex.Escape(name)}""\s*:\s*""[^""]*""",
                $"\"{name}\":\"{MaskToken}\"",
                RegexOptions.IgnoreCase);
        }

        return result;
    }

    private static object? MaskElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    p => p.Name,
                    p => IsSensitiveName(p.Name) && p.Value.ValueKind == JsonValueKind.String
                        ? (object?)MaskToken
                        : MaskElement(p.Value),
                    StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Select(MaskElement).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null,
        };
}

public sealed class EmailIntegrationSettings
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? Endpoint { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }
    public string? ReplyToEmail { get; set; }
    public string? TemplateNamespace { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 30;
}

public sealed class SmsIntegrationSettings
{
    public string? AccountId { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? Endpoint { get; set; }
    public string? SenderId { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 30;
    public string? DefaultCountryCode { get; set; }
}

public sealed class WhatsAppIntegrationSettings
{
    public string? AccessToken { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? Endpoint { get; set; }
    public string? WebhookVerifyToken { get; set; }
    public string? WebhookSecret { get; set; }
    public string? ApprovedTemplateNamespace { get; set; }
    public string DefaultLanguage { get; set; } = "ar";
    public int RequestTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Typed Map integration settings stored in PlatformIntegrationConfiguration.SettingsJson.
/// ServerApiKey never leaves the server; PublicBrowserToken may be returned only when intended for clients.
/// </summary>
public sealed class MapIntegrationSettings
{
    public string ProviderCode { get; set; } = IntegrationProviderCodes.LeafletOsm;

    /// <summary>Tile or style URL template for the browser (e.g. Leaflet {z}/{x}/{y}).</summary>
    public string? TileUrlTemplate { get; set; }

    /// <summary>Optional public browser token when the provider requires one in the client.</summary>
    public string? PublicBrowserToken { get; set; }

    /// <summary>Server-only secret — never included in public Map configuration DTOs.</summary>
    public string? ServerApiKey { get; set; }

    public string? StyleId { get; set; }

    public double DefaultLatitude { get; set; } = 30.0444;

    public double DefaultLongitude { get; set; } = 31.2357;

    public int DefaultZoom { get; set; } = 11;

    public int MinZoom { get; set; } = 5;

    public int MaxZoom { get; set; } = 18;

    public string? AttributionText { get; set; }

    public int RequestTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Typed Payment integration settings in PlatformIntegrationConfiguration.SettingsJson (Phase 1 plaintext).
/// Never return complete settings or secrets through APIs/logs.
/// </summary>
public sealed class PaymentIntegrationSettings
{
    public string Environment { get; set; } = "Sandbox";

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? MerchantId { get; set; }

    public string? AccountId { get; set; }

    public string? WebhookSecret { get; set; }

    public string? HostedCheckoutEndpoint { get; set; }

    public string? StatusQueryEndpoint { get; set; }

    public string? RefundEndpoint { get; set; }

    public string? ReturnUrlBase { get; set; }

    public string? TermsUrl { get; set; }

    public string? PrivacyUrl { get; set; }

    public string? LogoMediaKey { get; set; }

    public string[] SupportedCurrencies { get; set; } = ["EGP"];

    public string[] SupportedPaymentMethods { get; set; } = ["ProviderHostedCheckout"];

    public decimal MinimumAmount { get; set; }

    public decimal? MaximumAmount { get; set; }

    public bool DisplayProviderFees { get; set; }

    public bool DisplayProviderCommission { get; set; }

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Development-only scenario selector for SchooleraSandbox (success|pending|failed|cancel|expire).</summary>
    public string? SandboxDefaultOutcome { get; set; } = "success";
}

/// <summary>
/// Typed Financing integration settings in PlatformIntegrationConfiguration.SettingsJson (Phase 1 plaintext).
/// </summary>
public sealed class FinancingIntegrationSettings
{
    public string Environment { get; set; } = "Sandbox";

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? MerchantId { get; set; }

    public string? AccountId { get; set; }

    public string? WebhookSecret { get; set; }

    public string? OfferEndpoint { get; set; }

    public string? StatusQueryEndpoint { get; set; }

    public string? TermsUrl { get; set; }

    public string? PrivacyUrl { get; set; }

    public string? LogoMediaKey { get; set; }

    public string[] SupportedCurrencies { get; set; } = ["EGP"];

    public string[] SupportedFinancingMethods { get; set; } = ["Installments"];

    public int[] SupportedTenorsMonths { get; set; } = [3, 6, 12];

    public decimal MinimumAmount { get; set; }

    public decimal? MaximumAmount { get; set; }

    public bool DisplayProviderFees { get; set; } = true;

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Development-only: offers|decline|expire.</summary>
    public string? SandboxDefaultOutcome { get; set; } = "offers";
}

/// <summary>
/// Typed Meeting settings stored as Phase 1 plaintext JSON in the existing integration table.
/// Secrets are reserved for future dedicated real-provider adapters and are always redacted.
/// </summary>
public sealed class MeetingIntegrationSettings
{
    public string Environment { get; set; } = nameof(MeetingProviderEnvironment.Development);
    public string PublicNameAr { get; set; } = "اجتماع تجريبي";
    public string PublicNameEn { get; set; } = "Simulated meeting";
    public string? ApiBaseUrl { get; set; }
    public string? AccountId { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public int MaximumDurationMinutes { get; set; } = 180;
    public int JoinBeforeMinutes { get; set; } = 15;
    public int JoinAfterMinutes { get; set; }
    public int HostBeforeMinutes { get; set; } = 30;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public bool SupportsMeetingCreation { get; set; } = true;
    public bool SupportsParentAccess { get; set; } = true;
    public bool SupportsHostAccess { get; set; } = true;
    public bool SupportsCancellation { get; set; } = true;
    public bool SupportsStatusQuery { get; set; } = true;
    public string? TermsUrl { get; set; }
    public string? PrivacyUrl { get; set; }
    public string SimulatedScenario { get; set; } = nameof(SimulatedMeetingScenario.Success);
}

/// <summary>
/// Typed courier transport settings. Secrets remain server-only and must never be returned by
/// availability or health contracts.
/// </summary>
public sealed class CourierIntegrationSettings
{
    public string Environment { get; set; } = nameof(CourierProviderEnvironment.Simulated);
    public string? ApiBaseUrl { get; set; }
    public string? AccountId { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public string HealthScenario { get; set; } = nameof(SimulatedCourierHealthScenario.Healthy);
    public string AvailabilityScenario { get; set; } = nameof(SimulatedCourierAvailabilityScenario.Available);
}

public static class IntegrationSettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static T Deserialize<T>(string settingsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(settingsJson, Options)
                   ?? throw new InvalidOperationException("Settings JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Settings JSON is malformed.", ex);
        }
    }
}
