namespace Schoolera.Application.Cms.Constants;

public static class CmsSlugs
{
    /// <summary>Container category for interview/assessment FAQ items (not shown on general FAQ page).</summary>
    public const string InterviewFaqCategorySlug = "interview-assessment";

    public static readonly IReadOnlySet<string> SystemSlugs = new HashSet<string>(StringComparer.Ordinal)
    {
        "about",
        "how-it-works",
        "privacy",
        "terms",
        "sla",
    };

    public static readonly IReadOnlySet<string> ReservedSlugs = new HashSet<string>(StringComparer.Ordinal)
    {
        "api",
        "admin",
        "auth",
        "swagger",
        "uploads",
        "schools",
        "content",
        "contact",
        "faq",
        "home",
        "pages",
    };

    public static readonly IReadOnlySet<string> ReservedPrefixes = new HashSet<string>(StringComparer.Ordinal)
    {
        "admin-",
        "api-",
        "system-",
    };

    public static bool IsReserved(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return true;
        }

        var normalized = slug.Trim().ToLowerInvariant();
        if (ReservedSlugs.Contains(normalized))
        {
            return true;
        }

        return ReservedPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.Ordinal));
    }
}
