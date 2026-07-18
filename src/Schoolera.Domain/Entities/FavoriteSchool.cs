namespace Schoolera.Domain.Entities;

/// <summary>Parent-owned school favorite. Ownership key is ParentUserId (ApplicationUser.Id).</summary>
public sealed class FavoriteSchool
{
    private FavoriteSchool()
    {
    }

    public FavoriteSchool(Guid parentUserId, Guid schoolId)
    {
        Id = Guid.NewGuid();
        ParentUserId = parentUserId;
        SchoolId = schoolId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ParentUserId { get; private set; }

    public Guid SchoolId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
