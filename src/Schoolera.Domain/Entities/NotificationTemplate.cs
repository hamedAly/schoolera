using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Database notification template header (event + channel + culture).</summary>
public sealed class NotificationTemplate
{
    private readonly List<NotificationTemplateVersion> _versions = [];

    private NotificationTemplate()
    {
    }

    public NotificationTemplate(
        NotificationEventType eventType,
        NotificationChannel channel,
        string culture,
        string code)
    {
        if (!Enum.IsDefined(eventType))
        {
            throw new ArgumentOutOfRangeException(nameof(eventType));
        }

        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        Id = Guid.NewGuid();
        EventType = eventType;
        Channel = channel;
        Culture = NormalizeCulture(culture);
        Code = code.Trim();
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public NotificationEventType EventType { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public string Culture { get; private set; } = "ar";

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<NotificationTemplateVersion> Versions => _versions;

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public NotificationTemplateVersion AddVersion(
        string? subject,
        string body,
        string allowedVariablesCsv,
        string? providerTemplateId,
        Guid? createdByUserId)
    {
        var nextVersion = (_versions.Count == 0 ? 0 : _versions.Max(v => v.VersionNumber)) + 1;
        var version = new NotificationTemplateVersion(
            Id,
            nextVersion,
            subject,
            body,
            allowedVariablesCsv,
            providerTemplateId,
            createdByUserId);
        _versions.Add(version);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return version;
    }

    private static string NormalizeCulture(string culture)
    {
        var value = culture.Trim().ToLowerInvariant();
        if (value is not ("ar" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(culture), "Culture must be ar or en.");
        }

        return value;
    }
}

/// <summary>Immutable once published. Editing creates a new version.</summary>
public sealed class NotificationTemplateVersion
{
    private NotificationTemplateVersion()
    {
    }

    public NotificationTemplateVersion(
        Guid templateId,
        int versionNumber,
        string? subject,
        string body,
        string allowedVariablesCsv,
        string? providerTemplateId,
        Guid? createdByUserId)
    {
        if (versionNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Body is required.", nameof(body));
        }

        Id = Guid.NewGuid();
        TemplateId = templateId;
        VersionNumber = versionNumber;
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        Body = body.Trim();
        AllowedVariablesCsv = allowedVariablesCsv?.Trim() ?? string.Empty;
        ProviderTemplateId = string.IsNullOrWhiteSpace(providerTemplateId)
            ? null
            : providerTemplateId.Trim();
        IsPublished = false;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TemplateId { get; private set; }

    public NotificationTemplate Template { get; private set; } = null!;

    public int VersionNumber { get; private set; }

    public string? Subject { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string AllowedVariablesCsv { get; private set; } = string.Empty;

    public string? ProviderTemplateId { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Publish()
    {
        if (IsPublished)
        {
            return;
        }

        IsPublished = true;
        PublishedAtUtc = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<string> GetAllowedVariables() =>
        AllowedVariablesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
