namespace Schoolera.Domain.Entities;

/// <summary>Best-effort daily aggregate of public school profile views (no visitor identity).</summary>
public sealed class SchoolProfileViewDaily
{
    private SchoolProfileViewDaily()
    {
    }

    public SchoolProfileViewDaily(Guid schoolId, DateOnly viewDateUtc)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        ViewDateUtc = viewDateUtc;
        ViewCount = 1;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public DateOnly ViewDateUtc { get; private set; }

    public int ViewCount { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Increment()
    {
        ViewCount++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
