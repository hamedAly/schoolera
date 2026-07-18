namespace Schoolera.Domain.Enums;

/// <summary>Server-controlled platform integration categories.</summary>
public enum IntegrationType
{
    Email = 1,
    Sms = 2,
    WhatsApp = 3,
    /// <summary>Reserved for future approved integrations — not operational in Phase 1.</summary>
    Courier = 4,
    /// <summary>Reserved for future approved integrations — not operational in Phase 1.</summary>
    OtherExternalService = 5,
    /// <summary>Map tile/style provider for public School Search map view.</summary>
    Map = 6,
    /// <summary>Hosted payment checkout providers (Pay Now).</summary>
    Payment = 7,
    /// <summary>Consumer financing / installment offer providers.</summary>
    Financing = 8,
    /// <summary>Online interview/assessment meeting providers (Simulated/Development only in Phase 1).</summary>
    Meeting = 9,
}

public enum IntegrationHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
}

/// <summary>Well-known provider codes. New providers require typed validators.</summary>
public static class IntegrationProviderCodes
{
    public const string Development = "Development";
    public const string Simulated = "Simulated";
    public const string Smtp = "Smtp";

    /// <summary>
    /// Leaflet client with DB-configured tile template.
    /// Development seed may use OSM tiles; Production requires Product-approved tiles.
    /// </summary>
    public const string LeafletOsm = "LeafletOsm";

    /// <summary>Internal Development sandbox — never performs real financial transactions.</summary>
    public const string SchooleraSandbox = "SchooleraSandbox";

    public static bool IsSimulated(string? providerCode) =>
        string.Equals(providerCode, Development, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(providerCode, Simulated, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(providerCode, SchooleraSandbox, StringComparison.OrdinalIgnoreCase);
}
