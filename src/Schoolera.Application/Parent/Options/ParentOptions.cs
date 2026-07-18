namespace Schoolera.Application.Parent.Options;

public sealed class ParentChildOptions
{
    public const string SectionName = "ParentChild";

    /// <summary>Minimum age in years (inclusive) for school-age children.</summary>
    public int MinAgeYears { get; set; } = 3;

    /// <summary>Maximum age in years (inclusive) for school-age children.</summary>
    public int MaxAgeYears { get; set; } = 20;
}

public sealed class ParentIdentityProtectionOptions
{
    public const string SectionName = "ParentIdentityProtection";

    /// <summary>
    /// Base64-encoded 32+ byte key for HMAC-SHA256 identity lookup hashes.
    /// Production must supply via User Secrets / environment — never commit real keys.
    /// </summary>
    public string HmacKeyBase64 { get; set; } = string.Empty;
}
