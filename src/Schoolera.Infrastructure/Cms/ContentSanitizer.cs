using Ganss.Xss;
using Schoolera.Application.Common.Interfaces;
using System.Text.RegularExpressions;

namespace Schoolera.Infrastructure.Cms;

public sealed partial class ContentSanitizer : IContentSanitizer
{
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    public string SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitized = Sanitizer.Sanitize(html);
        return PostProcessLinks(sanitized);
    }

    public string StripToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitized = SanitizeHtml(html);
        var withoutTags = HtmlTagRegex().Replace(sanitized, " ");
        return System.Net.WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(
        [
            "p", "h2", "h3", "ul", "ol", "li", "strong", "em", "blockquote", "a", "br",
        ]);

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("title");
        sanitizer.AllowedAttributes.Add("rel");
        sanitizer.AllowedAttributes.Add("target");

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("tel");

        sanitizer.RemovingAttribute += (_, e) =>
        {
            if (e.Attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = false;
            }
        };

        return sanitizer;
    }

    private static string PostProcessLinks(string html)
    {
        return AnchorRegex().Replace(html, match =>
        {
            var href = match.Groups["href"].Value;
            if (string.IsNullOrWhiteSpace(href))
            {
                return match.Value;
            }

            var lower = href.Trim().ToLowerInvariant();
            if (lower.StartsWith("javascript:", StringComparison.Ordinal) ||
                lower.StartsWith("data:", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(href, UriKind.Absolute, out var absolute) &&
                (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                var rel = "noopener noreferrer";
                if (match.Value.Contains("rel=", StringComparison.OrdinalIgnoreCase))
                {
                    return match.Value;
                }

                return match.Value.Replace(
                    ">",
                    $" rel=\"{rel}\" target=\"_blank\">",
                    StringComparison.Ordinal);
            }

            return match.Value;
        });
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("<a\\s+[^>]*href\\s*=\\s*[\"'](?<href>[^\"']*)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AnchorRegex();
}
