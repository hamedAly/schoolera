namespace Schoolera.Application.Cms.Common;

public static class CmsUrlValidator
{
    public static bool IsValidCtaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        if (trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return !trimmed.Contains("..", StringComparison.Ordinal);
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
        {
            return false;
        }

        if (!string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !IsDangerousScheme(trimmed);
    }

    private static bool IsDangerousScheme(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        return lower.StartsWith("javascript:", StringComparison.Ordinal) ||
               lower.StartsWith("data:", StringComparison.Ordinal) ||
               lower.StartsWith("vbscript:", StringComparison.Ordinal);
    }
}
