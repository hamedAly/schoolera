namespace Schoolera.Domain.Entities;

public sealed class AcademicYear
{
    private AcademicYear()
    {
    }

    public AcademicYear(
        string nameAr,
        string? nameEn,
        string slug,
        DateOnly startDate,
        DateOnly endDate,
        bool isCurrent)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public bool IsCurrent { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string nameAr,
        string? nameEn,
        string slug,
        DateOnly startDate,
        DateOnly endDate,
        bool isCurrent,
        bool isActive)
    {
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
