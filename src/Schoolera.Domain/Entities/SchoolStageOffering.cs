using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class SchoolStageOffering
{
    private SchoolStageOffering()
    {
    }

    public SchoolStageOffering(
        Guid schoolBranchId,
        Guid educationalStageId,
        GenderType genderType,
        int? capacity,
        bool isAdmissionOpen)
    {
        Id = Guid.NewGuid();
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GenderType = genderType;
        Capacity = capacity;
        IsAdmissionOpen = isAdmissionOpen;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SchoolBranchId { get; private set; }

    public SchoolBranch SchoolBranch { get; private set; } = null!;

    public Guid EducationalStageId { get; private set; }

    public EducationalStage EducationalStage { get; private set; } = null!;

    public GenderType GenderType { get; private set; }

    public int? Capacity { get; private set; }

    public bool IsAdmissionOpen { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<SchoolGradeOffering> GradeOfferings { get; private set; } = [];

    public void Update(GenderType genderType, int? capacity, bool isAdmissionOpen)
    {
        GenderType = genderType;
        Capacity = capacity;
        IsAdmissionOpen = isAdmissionOpen;
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

    public void AddGradeOffering(SchoolGradeOffering gradeOffering)
    {
        GradeOfferings.Add(gradeOffering);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
