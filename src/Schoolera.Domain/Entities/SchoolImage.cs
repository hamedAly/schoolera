namespace Schoolera.Domain.Entities;

public sealed class SchoolImage
{
    private SchoolImage()
    {
    }

    public SchoolImage(Guid schoolId, string imageUrl, int sortOrder)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        ImageUrl = imageUrl.Trim();
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public Guid? SchoolBranchId { get; private set; }

    public string ImageUrl { get; private set; } = string.Empty;

    public string? CaptionAr { get; private set; }

    public string? CaptionEn { get; private set; }

    public int SortOrder { get; private set; }

    public Guid? EducationalStageId { get; private set; }

    public string? AltTextAr { get; private set; }

    public string? AltTextEn { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdateMetadata(
        string? captionAr,
        string? captionEn,
        string? altTextAr,
        string? altTextEn,
        Guid? educationalStageId,
        int sortOrder)
    {
        CaptionAr = string.IsNullOrWhiteSpace(captionAr) ? null : captionAr.Trim();
        CaptionEn = string.IsNullOrWhiteSpace(captionEn) ? null : captionEn.Trim();
        AltTextAr = string.IsNullOrWhiteSpace(altTextAr) ? null : altTextAr.Trim();
        AltTextEn = string.IsNullOrWhiteSpace(altTextEn) ? null : altTextEn.Trim();
        EducationalStageId = educationalStageId;
        SortOrder = sortOrder;
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
}
