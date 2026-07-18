namespace Schoolera.Domain.Entities;

public sealed class SchoolGradeOffering
{
    private SchoolGradeOffering()
    {
    }

    public SchoolGradeOffering(Guid schoolStageOfferingId, Guid gradeId)
    {
        Id = Guid.NewGuid();
        SchoolStageOfferingId = schoolStageOfferingId;
        GradeId = gradeId;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolStageOfferingId { get; private set; }

    public SchoolStageOffering SchoolStageOffering { get; private set; } = null!;

    public Guid GradeId { get; private set; }

    public Grade Grade { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
