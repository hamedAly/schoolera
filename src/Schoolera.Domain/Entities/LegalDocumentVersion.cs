using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Immutable published snapshot of Terms or Privacy for a culture.
/// New publishes append versions; old rows are never overwritten.
/// </summary>
public sealed class LegalDocumentVersion
{
    private LegalDocumentVersion()
    {
    }

    public LegalDocumentVersion(
        LegalDocumentType documentType,
        string culture,
        int versionNumber,
        string title,
        string content,
        DateTimeOffset publishedAtUtc,
        bool isCurrentMandatory)
    {
        Id = Guid.NewGuid();
        DocumentType = documentType;
        Culture = NormalizeCulture(culture);
        VersionNumber = versionNumber;
        Title = title.Trim();
        Content = content.Trim();
        PublishedAtUtc = publishedAtUtc;
        IsCurrentMandatory = isCurrentMandatory;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public LegalDocumentType DocumentType { get; private set; }

    /// <summary>UI culture tag: <c>ar</c> or <c>en</c>.</summary>
    public string Culture { get; private set; } = string.Empty;

    public int VersionNumber { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset PublishedAtUtc { get; private set; }

    public bool IsCurrentMandatory { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void ClearCurrentMandatory()
    {
        IsCurrentMandatory = false;
    }

    private static string NormalizeCulture(string culture)
    {
        var normalized = culture.Trim().ToLowerInvariant();
        return normalized.StartsWith("en", StringComparison.Ordinal) ? "en" : "ar";
    }
}
