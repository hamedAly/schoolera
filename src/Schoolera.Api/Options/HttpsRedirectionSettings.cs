namespace Schoolera.Api.Options;

/// <summary>
/// Controls HTTPS redirection. In Development the HTTP-only Angular proxy workflow
/// must not be forced onto a different HTTPS port (for example 7076).
/// </summary>
public sealed class HttpsRedirectionSettings
{
    public const string SectionName = "HttpsRedirection";

    /// <summary>
    /// When false, <c>UseHttpsRedirection</c> is skipped. Production defaults to true.
    /// Development defaults to false so <c>http://localhost:5085</c> stays on HTTP.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
