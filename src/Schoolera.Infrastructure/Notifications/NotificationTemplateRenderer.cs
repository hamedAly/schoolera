using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Notifications;

public sealed partial class NotificationTemplateRenderer(SchooleraDbContext dbContext)
    : INotificationTemplateRenderer
{
    private static readonly Regex PlaceholderRegex = VariablePlaceholderRegex();

    public (string? Subject, string Body, Guid VersionId, int VersionNumber, string TemplateCode)? TryRender(
        NotificationEventType eventType,
        NotificationChannel channel,
        string culture,
        IReadOnlyDictionary<string, string> variables)
    {
        var normalizedCulture = culture.Trim().ToLowerInvariant();
        if (normalizedCulture is not ("ar" or "en"))
        {
            normalizedCulture = "ar";
        }

        var template = dbContext.NotificationTemplates
            .AsNoTracking()
            .Include(item => item.Versions)
            .Where(item =>
                item.EventType == eventType &&
                item.Channel == channel &&
                item.Culture == normalizedCulture &&
                item.IsActive)
            .FirstOrDefault();

        if (template is null)
        {
            return null;
        }

        var version = template.Versions
            .Where(item => item.IsPublished)
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefault();

        if (version is null)
        {
            return null;
        }

        var allowed = version.GetAllowedVariables()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!TryReplace(version.Subject, allowed, variables, out var subject) ||
            !TryReplace(version.Body, allowed, variables, out var body) ||
            body is null)
        {
            return null;
        }

        return (subject, body, version.Id, version.VersionNumber, template.Code);
    }

    private static bool TryReplace(
        string? template,
        ISet<string> allowed,
        IReadOnlyDictionary<string, string> variables,
        out string? rendered)
    {
        if (template is null)
        {
            rendered = null;
            return true;
        }

        var unknownFound = false;
        rendered = PlaceholderRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (!allowed.Contains(name))
            {
                unknownFound = true;
                return match.Value;
            }

            if (variables.TryGetValue(name, out var value))
            {
                return value;
            }

            var ignoreCase = variables
                .FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase));
            return ignoreCase.Key is null ? string.Empty : ignoreCase.Value;
        });

        if (unknownFound)
        {
            rendered = null;
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"\{\{\s*([A-Za-z][A-Za-z0-9_]*)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePlaceholderRegex();
}
