namespace Schoolera.Domain.Entities;

public sealed class FaqCategory
{
    private FaqCategory()
    {
    }

    public FaqCategory(string nameAr, string nameEn, string slug, int sortOrder)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr.Trim();
        NameEn = nameEn.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        SortOrder = sortOrder;
        IsPublished = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        Items = new List<FaqItem>();
    }

    public Guid Id { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<FaqItem> Items { get; private set; } = new List<FaqItem>();

    public void Update(string nameAr, string nameEn, string slug)
    {
        NameAr = nameAr.Trim();
        NameEn = nameEn.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Touch();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        Touch();
    }

    public void Publish()
    {
        IsPublished = true;
        Touch();
    }

    public void Unpublish()
    {
        IsPublished = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
