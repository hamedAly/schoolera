using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Shared slug normalization for domain seeding and application services.
/// </summary>
public static partial class SlugHelper
{
    private static readonly Regex InvalidCharsRegex = InvalidChars();
    private static readonly Regex WhitespaceRegex = Whitespace();
    private static readonly Regex MultiDashRegex = MultiDash();

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Slug source cannot be empty.", nameof(input));
        }

        var trimmed = input.Trim();
        if (ContainsPathTraversal(trimmed))
        {
            throw new ArgumentException("Slug source contains invalid path characters.", nameof(input));
        }

        var latin = FromLatinText(trimmed);
        if (!string.IsNullOrWhiteSpace(latin) && IsAsciiSlug(latin))
        {
            return latin;
        }

        // Arabic or mixed text: transliterate minimally via hash-based stable slug.
        return $"item-{CreateStableHash(trimmed)}";
    }

    public static string FromLatinText(string input)
    {
        var lower = input.ToLowerInvariant().Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.')
            {
                builder.Append('-');
            }
        }

        var collapsed = MultiDashRegex.Replace(
            WhitespaceRegex.Replace(builder.ToString(), "-"),
            "-").Trim('-');

        return collapsed;
    }

    public static bool IsValidSlug(string slug) =>
        !string.IsNullOrWhiteSpace(slug) &&
        !ContainsPathTraversal(slug) &&
        slug.Length <= Common.FieldLengthLimits.Slug &&
        IsAsciiSlug(slug);

    private static bool IsAsciiSlug(string slug) =>
        slug.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static bool ContainsPathTraversal(string value) =>
        value.Contains('/', StringComparison.Ordinal) ||
        value.Contains('\\', StringComparison.Ordinal) ||
        value.Contains("..", StringComparison.Ordinal);

    private static string CreateStableHash(string input)
    {
        var hash = input.GetHashCode(StringComparison.Ordinal);
        return Math.Abs(hash).ToString(CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"[^\w\-]+", RegexOptions.Compiled)]
    private static partial Regex InvalidChars();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultiDash();
}
