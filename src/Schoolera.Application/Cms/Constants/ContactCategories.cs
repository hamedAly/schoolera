namespace Schoolera.Application.Cms.Constants;

public static class ContactCategories
{
    public const string General = "general";
    public const string ParentSupport = "parent-support";
    public const string SchoolPartnership = "school-partnership";
    public const string Technical = "technical";
    public const string Billing = "billing";
    public const string Other = "other";

    private static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        General,
        ParentSupport,
        SchoolPartnership,
        Technical,
        Billing,
        Other,
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
