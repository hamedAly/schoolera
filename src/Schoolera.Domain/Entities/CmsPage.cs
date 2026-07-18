using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Managed static CMS page (About, Privacy, custom published pages).</summary>
public sealed class CmsPage
{
    private CmsPage()
    {
    }

    public CmsPage(
        string slug,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string? metaTitleAr,
        string? metaTitleEn,
        string? metaDescriptionAr,
        string? metaDescriptionEn,
        bool isSystemPage)
    {
        Id = Guid.NewGuid();
        Slug = NormalizeSlug(slug);
        TitleAr = titleAr.Trim();
        TitleEn = titleEn.Trim();
        ContentAr = contentAr.Trim();
        ContentEn = contentEn.Trim();
        MetaTitleAr = NormalizeOptional(metaTitleAr);
        MetaTitleEn = NormalizeOptional(metaTitleEn);
        MetaDescriptionAr = NormalizeOptional(metaDescriptionAr);
        MetaDescriptionEn = NormalizeOptional(metaDescriptionEn);
        Status = CmsPublicationStatus.Draft;
        IsSystemPage = isSystemPage;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string TitleAr { get; private set; } = string.Empty;

    public string TitleEn { get; private set; } = string.Empty;

    public string ContentAr { get; private set; } = string.Empty;

    public string ContentEn { get; private set; } = string.Empty;

    public string? MetaTitleAr { get; private set; }

    public string? MetaTitleEn { get; private set; }

    public string? MetaDescriptionAr { get; private set; }

    public string? MetaDescriptionEn { get; private set; }

    public CmsPublicationStatus Status { get; private set; }

    public bool IsSystemPage { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void UpdateContent(
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string? metaTitleAr,
        string? metaTitleEn,
        string? metaDescriptionAr,
        string? metaDescriptionEn)
    {
        TitleAr = titleAr.Trim();
        TitleEn = titleEn.Trim();
        ContentAr = contentAr.Trim();
        ContentEn = contentEn.Trim();
        MetaTitleAr = NormalizeOptional(metaTitleAr);
        MetaTitleEn = NormalizeOptional(metaTitleEn);
        MetaDescriptionAr = NormalizeOptional(metaDescriptionAr);
        MetaDescriptionEn = NormalizeOptional(metaDescriptionEn);
        Touch();
    }

    public void RenameSlug(string slug)
    {
        if (IsSystemPage)
        {
            throw new InvalidOperationException("System page slugs cannot be renamed.");
        }

        Slug = NormalizeSlug(slug);
        Touch();
    }

    public void Publish()
    {
        Status = CmsPublicationStatus.Published;
        PublishedAtUtc ??= DateTimeOffset.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        Status = CmsPublicationStatus.Draft;
        Touch();
    }

    public void Archive()
    {
        Status = CmsPublicationStatus.Archived;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
