namespace Schoolera.Domain.Entities;

public sealed class Curriculum
{
    private Curriculum()
    {
    }

    public Curriculum(string nameAr, string? nameEn, string slug, int sortOrder)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string nameAr,
        string? nameEn,
        string slug,
        string? descriptionAr,
        string? descriptionEn,
        int sortOrder,
        bool isActive)
    {
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        DescriptionAr = string.IsNullOrWhiteSpace(descriptionAr) ? null : descriptionAr.Trim();
        DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
