namespace Schoolera.Application.Cms.Constants;

public static class ContactSources
{
    public const string ContactPage = "contact-page";
    public const string Homepage = "homepage";
    public const string Footer = "footer";
    public const string Help = "help";

    private static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        ContactPage,
        Homepage,
        Footer,
        Help,
    };

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return Allowed.Contains(normalized) ? normalized : null;
    }

    public static IReadOnlyCollection<string> All => Allowed;
}
