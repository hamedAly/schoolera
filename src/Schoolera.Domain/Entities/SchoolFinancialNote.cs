namespace Schoolera.Domain.Entities;

/// <summary>
/// Bounded school financial note. Internal notes must never appear on public/Parent DTOs.
/// </summary>
public sealed class SchoolFinancialNote
{
    private SchoolFinancialNote()
    {
    }

    public SchoolFinancialNote(
        Guid schoolId,
        string textAr,
        string? textEn,
        bool isInternal,
        int sortOrder)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TextAr = textAr.Trim();
        TextEn = string.IsNullOrWhiteSpace(textEn) ? null : textEn.Trim();
        IsInternal = isInternal;
        SortOrder = sortOrder;
        IsActive = true;
        IsPublished = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public string TextAr { get; private set; } = string.Empty;

    public string? TextEn { get; private set; }

    public bool IsInternal { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(string textAr, string? textEn, bool isInternal, int sortOrder)
    {
        TextAr = textAr.Trim();
        TextEn = string.IsNullOrWhiteSpace(textEn) ? null : textEn.Trim();
        IsInternal = isInternal;
        SortOrder = sortOrder;
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

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
