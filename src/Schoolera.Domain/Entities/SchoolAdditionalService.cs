namespace Schoolera.Domain.Entities;

/// <summary>School-level additional service (transport, meals, activities, etc.).</summary>
public sealed class SchoolAdditionalService
{
    private SchoolAdditionalService()
    {
    }

    public SchoolAdditionalService(
        Guid schoolId,
        string nameAr,
        string? nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string? iconKey,
        int sortOrder)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        ApplyContent(nameAr, nameEn, descriptionAr, descriptionEn, iconKey, sortOrder);
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public string? IconKey { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string nameAr,
        string? nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string? iconKey,
        int sortOrder)
    {
        ApplyContent(nameAr, nameEn, descriptionAr, descriptionEn, iconKey, sortOrder);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void ApplyContent(
        string nameAr,
        string? nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string? iconKey,
        int sortOrder)
    {
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        DescriptionAr = string.IsNullOrWhiteSpace(descriptionAr) ? null : descriptionAr.Trim();
        DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? null : iconKey.Trim();
        SortOrder = sortOrder;
    }
}
