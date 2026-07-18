namespace Schoolera.Domain.Entities;

public sealed class SchoolCurriculum
{
    private SchoolCurriculum()
    {
    }

    public SchoolCurriculum(Guid schoolId, Guid curriculumId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        CurriculumId = curriculumId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public Guid CurriculumId { get; private set; }

    public Curriculum Curriculum { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
