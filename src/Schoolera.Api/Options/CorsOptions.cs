namespace Schoolera.Api.Options;

/// <summary>
/// Development CORS origins for Angular <c>ng serve</c>. Production remains same-origin
/// and does not use this policy.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Exact frontend origins allowed to call the API with credentials in Development
    /// (for example <c>http://localhost:5100</c>). Never use wildcards here.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
