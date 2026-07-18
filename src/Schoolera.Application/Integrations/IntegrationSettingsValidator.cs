using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Integrations;

public sealed record IntegrationSettingsValidationResult(
    bool IsValid,
    IReadOnlyList<string> ErrorCodes);

public interface IIntegrationSettingsValidator
{
    IntegrationSettingsValidationResult Validate(
        IntegrationType integrationType,
        string providerCode,
        string settingsJson,
        int settingsSchemaVersion);
}

public sealed class IntegrationSettingsValidator : IIntegrationSettingsValidator
{
    public IntegrationSettingsValidationResult Validate(
        IntegrationType integrationType,
        string providerCode,
        string settingsJson,
        int settingsSchemaVersion)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(integrationType) ||
            integrationType is IntegrationType.OtherExternalService)
        {
            if (integrationType is IntegrationType.OtherExternalService)
            {
                errors.Add("integrations.providerNotOperational");
            }
            else if (!Enum.IsDefined(integrationType))
            {
                errors.Add("integrations.invalidType");
            }
        }

        if (settingsSchemaVersion < 1)
        {
            errors.Add("integrations.invalidSchemaVersion");
        }

        if (string.IsNullOrWhiteSpace(providerCode))
        {
            errors.Add("integrations.invalidProviderCode");
        }

        try
        {
            using var _ = JsonDocument.Parse(settingsJson);
        }
        catch
        {
            return new IntegrationSettingsValidationResult(false, ["integrations.invalidJson"]);
        }

        if (IntegrationProviderCodes.IsSimulated(providerCode) &&
            integrationType is not IntegrationType.Map and
            not IntegrationType.Payment and
            not IntegrationType.Financing and
            not IntegrationType.Meeting and
            not IntegrationType.Courier)
        {
            return new IntegrationSettingsValidationResult(errors.Count == 0, errors);
        }

        switch (integrationType)
        {
            case IntegrationType.Email:
                ValidateEmail(providerCode, settingsJson, errors);
                break;
            case IntegrationType.Sms:
                ValidateSms(providerCode, settingsJson, errors);
                break;
            case IntegrationType.WhatsApp:
                ValidateWhatsApp(providerCode, settingsJson, errors);
                break;
            case IntegrationType.Map:
                ValidateMap(providerCode, settingsJson, errors);
                break;
            case IntegrationType.Payment:
                ValidatePayment(providerCode, settingsJson, errors);
                break;
            case IntegrationType.Financing:
                ValidateFinancing(providerCode, settingsJson, errors);
                break;
            case IntegrationType.Meeting:
                ValidateMeeting(providerCode, settingsJson, errors);
                break;
            case IntegrationType.Courier:
                ValidateCourier(providerCode, settingsJson, errors);
                break;
        }

        return new IntegrationSettingsValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateCourier(string providerCode, string json, List<string> errors)
    {
        var isSimulatedProvider =
            string.Equals(providerCode, IntegrationProviderCodes.Simulated, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(providerCode, IntegrationProviderCodes.Development, StringComparison.OrdinalIgnoreCase);
        if (!isSimulatedProvider)
        {
            errors.Add("integrations.courier.providerNotOperational");
        }

        CourierIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<CourierIntegrationSettings>(json);
        }
        catch
        {
            errors.Add("integrations.invalidJson");
            return;
        }

        if (!Enum.TryParse<CourierProviderEnvironment>(
                settings.Environment,
                ignoreCase: true,
                out var providerEnvironment) ||
            !Enum.IsDefined(providerEnvironment))
        {
            errors.Add("integrations.courier.invalidEnvironment");
            return;
        }

        if (isSimulatedProvider && providerEnvironment != CourierProviderEnvironment.Simulated)
        {
            errors.Add("integrations.courier.simulatedEnvironmentRequired");
        }

        if (providerEnvironment is CourierProviderEnvironment.Sandbox or CourierProviderEnvironment.Production &&
            !errors.Contains("integrations.courier.providerNotOperational", StringComparer.Ordinal))
        {
            errors.Add("integrations.courier.providerNotOperational");
        }

        if (settings.RequestTimeoutSeconds is < 1 or > 120)
        {
            errors.Add("integrations.invalidTimeout");
        }

        if (settings.MaxRetryAttempts is < 0 or > 10 ||
            settings.RetryDelaySeconds is < 1 or > 3600)
        {
            errors.Add("integrations.courier.invalidRetry");
        }

        if (!Enum.TryParse<SimulatedCourierHealthScenario>(settings.HealthScenario, true, out _) ||
            !Enum.TryParse<SimulatedCourierAvailabilityScenario>(settings.AvailabilityScenario, true, out _))
        {
            errors.Add("integrations.courier.invalidScenario");
        }

        ValidateCourierUrl(settings.ApiBaseUrl, providerEnvironment, errors);
        ValidateBoundedOptional(settings.AccountId, 200, "integrations.courier.invalidAccountId", errors);
        ValidateBoundedOptional(settings.ApiKey, 2000, "integrations.courier.invalidSecret", errors);
        ValidateBoundedOptional(settings.ApiSecret, 2000, "integrations.courier.invalidSecret", errors);
        ValidateBoundedOptional(settings.AccessToken, 4000, "integrations.courier.invalidSecret", errors);
        ValidateBoundedOptional(settings.WebhookSecret, 2000, "integrations.courier.invalidSecret", errors);
    }

    private static void ValidateCourierUrl(
        string? value,
        CourierProviderEnvironment environment,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Length > 2048 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            (environment is CourierProviderEnvironment.Sandbox or CourierProviderEnvironment.Production &&
             uri.Scheme != Uri.UriSchemeHttps) ||
            (environment == CourierProviderEnvironment.Production && IsUnsafeProductionHost(uri.Host)))
        {
            errors.Add("integrations.courier.invalidApiBaseUrl");
        }
    }

    private static bool IsUnsafeProductionHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal)
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
        {
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   bytes[0] == 169 && bytes[1] == 254 ||
                   bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                   bytes[0] == 192 && bytes[1] == 168;
        }

        return bytes.Length == 16 && (bytes[0] & 0xFE) == 0xFC;
    }

    private static void ValidateBoundedOptional(
        string? value,
        int maximumLength,
        string errorCode,
        List<string> errors)
    {
        if (value?.Length > maximumLength)
        {
            errors.Add(errorCode);
        }
    }

    private static void ValidateMeeting(string providerCode, string json, List<string> errors)
    {
        if (!string.Equals(providerCode, IntegrationProviderCodes.Development, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(providerCode, IntegrationProviderCodes.Simulated, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("integrations.providerNotConfigured");
            return;
        }

        MeetingIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<MeetingIntegrationSettings>(json);
        }
        catch
        {
            errors.Add("integrations.invalidJson");
            return;
        }

        if (!Enum.TryParse<MeetingProviderEnvironment>(
                settings.Environment, true, out var environment) ||
            environment is not (MeetingProviderEnvironment.Development or MeetingProviderEnvironment.Test))
            errors.Add("integrations.meeting.simulatedDevelopmentOnly");
        if (string.IsNullOrWhiteSpace(settings.PublicNameAr) ||
            string.IsNullOrWhiteSpace(settings.PublicNameEn))
            errors.Add("integrations.meeting.publicNamesRequired");
        if (!settings.SupportsMeetingCreation)
            errors.Add("integrations.meeting.creationCapabilityRequired");
        if (settings.MaximumDurationMinutes is < 5 or > 480 ||
            settings.JoinBeforeMinutes is < 0 or > 120 ||
            settings.JoinAfterMinutes is < 0 or > 120 ||
            settings.HostBeforeMinutes is < 0 or > 240)
            errors.Add("integrations.meeting.invalidWindows");
        if (settings.MaxRetryAttempts is < 0 or > 10 ||
            settings.RetryDelaySeconds is < 1 or > 3600)
            errors.Add("integrations.meeting.invalidRetry");
        if (!Enum.TryParse<SimulatedMeetingScenario>(
                settings.SimulatedScenario, true, out _))
            errors.Add("integrations.meeting.invalidSimulatedScenario");
        ValidateTimeout(settings.RequestTimeoutSeconds, errors);
        ValidateOptionalUrl(settings.TermsUrl, errors);
        ValidateOptionalUrl(settings.PrivacyUrl, errors);
    }

    private static void ValidatePayment(string providerCode, string json, List<string> errors)
    {
        if (!string.Equals(providerCode, IntegrationProviderCodes.SchooleraSandbox, StringComparison.OrdinalIgnoreCase) &&
            !IntegrationProviderCodes.IsSimulated(providerCode))
        {
            errors.Add("integrations.providerNotConfigured");
            return;
        }

        PaymentIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<PaymentIntegrationSettings>(json);
        }
        catch
        {
            errors.Add("integrations.invalidJson");
            return;
        }

        if (settings.Environment is not ("Sandbox" or "Production"))
        {
            errors.Add("integrations.payment.invalidEnvironment");
        }

        if (settings.Environment == "Production" &&
            string.Equals(providerCode, IntegrationProviderCodes.SchooleraSandbox, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("integrations.payment.sandboxCannotBeProduction");
        }

        if (settings.MinimumAmount is < 0 ||
            (settings.MaximumAmount is not null && settings.MinimumAmount > settings.MaximumAmount))
        {
            errors.Add("integrations.payment.invalidAmountLimits");
        }

        ValidateTimeout(settings.RequestTimeoutSeconds, errors);
        ValidateOptionalUrl(settings.HostedCheckoutEndpoint, errors);
        ValidateOptionalUrl(settings.StatusQueryEndpoint, errors);
        ValidateOptionalUrl(settings.RefundEndpoint, errors);
        ValidateOptionalUrl(settings.ReturnUrlBase, errors);
        ValidateOptionalUrl(settings.TermsUrl, errors);
        ValidateOptionalUrl(settings.PrivacyUrl, errors);
    }

    private static void ValidateFinancing(string providerCode, string json, List<string> errors)
    {
        if (!string.Equals(providerCode, IntegrationProviderCodes.SchooleraSandbox, StringComparison.OrdinalIgnoreCase) &&
            !IntegrationProviderCodes.IsSimulated(providerCode))
        {
            errors.Add("integrations.providerNotConfigured");
            return;
        }

        FinancingIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<FinancingIntegrationSettings>(json);
        }
        catch
        {
            errors.Add("integrations.invalidJson");
            return;
        }

        if (settings.Environment is not ("Sandbox" or "Production"))
        {
            errors.Add("integrations.financing.invalidEnvironment");
        }

        if (settings.Environment == "Production" &&
            string.Equals(providerCode, IntegrationProviderCodes.SchooleraSandbox, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("integrations.financing.sandboxCannotBeProduction");
        }

        if (settings.MinimumAmount is < 0 ||
            (settings.MaximumAmount is not null && settings.MinimumAmount > settings.MaximumAmount))
        {
            errors.Add("integrations.financing.invalidAmountLimits");
        }

        ValidateTimeout(settings.RequestTimeoutSeconds, errors);
        ValidateOptionalUrl(settings.OfferEndpoint, errors);
        ValidateOptionalUrl(settings.StatusQueryEndpoint, errors);
        ValidateOptionalUrl(settings.TermsUrl, errors);
        ValidateOptionalUrl(settings.PrivacyUrl, errors);
    }

    private static void ValidateEmail(string providerCode, string json, List<string> errors)
    {
        if (!string.Equals(providerCode, IntegrationProviderCodes.Smtp, StringComparison.OrdinalIgnoreCase) &&
            !IntegrationProviderCodes.IsSimulated(providerCode))
        {
            errors.Add("integrations.unsupportedProviderCode");
            return;
        }

        var settings = IntegrationSettingsSerializer.Deserialize<EmailIntegrationSettings>(json);
        if (string.Equals(providerCode, IntegrationProviderCodes.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
            {
                errors.Add("integrations.email.hostRequired");
            }

            if (string.IsNullOrWhiteSpace(settings.SenderEmail) ||
                !new EmailAddressAttribute().IsValid(settings.SenderEmail))
            {
                errors.Add("integrations.email.senderRequired");
            }

            if (settings.Port is < 1 or > 65535)
            {
                errors.Add("integrations.email.invalidPort");
            }
        }

        ValidateTimeout(settings.RequestTimeoutSeconds, errors);
        ValidateOptionalUrl(settings.Endpoint, errors);
    }

    private static void ValidateSms(string providerCode, string json, List<string> errors)
    {
        if (IntegrationProviderCodes.IsSimulated(providerCode))
        {
            return;
        }

        errors.Add("integrations.unsupportedProviderCode");
        _ = IntegrationSettingsSerializer.Deserialize<SmsIntegrationSettings>(json);
    }

    private static void ValidateWhatsApp(string providerCode, string json, List<string> errors)
    {
        if (IntegrationProviderCodes.IsSimulated(providerCode))
        {
            return;
        }

        errors.Add("integrations.unsupportedProviderCode");
        _ = IntegrationSettingsSerializer.Deserialize<WhatsAppIntegrationSettings>(json);
    }

    private static void ValidateMap(string providerCode, string json, List<string> errors)
    {
        if (!string.Equals(providerCode, IntegrationProviderCodes.LeafletOsm, StringComparison.OrdinalIgnoreCase) &&
            !IntegrationProviderCodes.IsSimulated(providerCode))
        {
            errors.Add("integrations.unsupportedProviderCode");
            return;
        }

        MapIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<MapIntegrationSettings>(json);
        }
        catch
        {
            errors.Add("integrations.invalidJson");
            return;
        }

        if (IntegrationProviderCodes.IsSimulated(providerCode))
        {
            ValidateTimeout(settings.RequestTimeoutSeconds, errors);
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.TileUrlTemplate) ||
            !Uri.TryCreate(
                settings.TileUrlTemplate
                    .Replace("{z}", "0", StringComparison.Ordinal)
                    .Replace("{x}", "0", StringComparison.Ordinal)
                    .Replace("{y}", "0", StringComparison.Ordinal)
                    .Replace("{s}", "a", StringComparison.Ordinal),
                UriKind.Absolute,
                out var tileUri) ||
            (tileUri.Scheme != Uri.UriSchemeHttps && tileUri.Scheme != Uri.UriSchemeHttp))
        {
            errors.Add("integrations.map.invalidTileUrl");
        }

        if (settings.DefaultLatitude is < -90 or > 90 ||
            settings.DefaultLongitude is < -180 or > 180)
        {
            errors.Add("integrations.map.invalidDefaultCenter");
        }

        if (settings.MinZoom < 0 ||
            settings.MaxZoom > 22 ||
            settings.MinZoom > settings.MaxZoom ||
            settings.DefaultZoom < settings.MinZoom ||
            settings.DefaultZoom > settings.MaxZoom)
        {
            errors.Add("integrations.map.invalidZoomRange");
        }

        if (string.IsNullOrWhiteSpace(settings.AttributionText))
        {
            errors.Add("integrations.map.attributionRequired");
        }

        ValidateTimeout(settings.RequestTimeoutSeconds, errors);
    }

    private static void ValidateTimeout(int seconds, List<string> errors)
    {
        if (seconds is < 1 or > 120)
        {
            errors.Add("integrations.invalidTimeout");
        }
    }

    private static void ValidateOptionalUrl(string? endpoint, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            errors.Add("integrations.invalidEndpoint");
        }
    }
}
