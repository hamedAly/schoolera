namespace Schoolera.Domain.Entities;

public sealed class SchoolFacility
{
    private SchoolFacility()
    {
    }

    public SchoolFacility(Guid schoolId, Guid facilityId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        FacilityId = facilityId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public Guid FacilityId { get; private set; }

    public Facility Facility { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
