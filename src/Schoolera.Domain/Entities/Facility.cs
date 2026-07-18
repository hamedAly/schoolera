namespace Schoolera.Domain.Entities;

public sealed class Facility
{
    private Facility()
    {
    }

    public Facility(string nameAr, string? nameEn, string slug, int sortOrder, string? iconKey)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? null : iconKey.Trim();
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string? IconKey { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string nameAr,
        string? nameEn,
        string slug,
        string? iconKey,
        int sortOrder,
        bool isActive)
    {
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? null : iconKey.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
