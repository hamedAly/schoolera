using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Parent-owned "notify me when admissions open" subscription.</summary>
public sealed class ParentAdmissionOpenSubscription
{
    private ParentAdmissionOpenSubscription()
    {
    }

    public ParentAdmissionOpenSubscription(
        Guid parentUserId,
        Guid schoolId,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        NotificationChannel preferredChannel)
    {
        if (!Enum.IsDefined(preferredChannel))
        {
            throw new ArgumentOutOfRangeException(nameof(preferredChannel));
        }

        Id = Guid.NewGuid();
        ParentUserId = parentUserId;
        SchoolId = schoolId;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        PreferredChannel = preferredChannel;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ParentUserId { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid? SchoolBranchId { get; private set; }

    public Guid? EducationalStageId { get; private set; }

    public Guid? GradeId { get; private set; }

    public Guid? AcademicYearId { get; private set; }

    public NotificationChannel PreferredChannel { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? UnsubscribedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void UpdatePreferredChannel(NotificationChannel channel)
    {
        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        PreferredChannel = channel;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Unsubscribe()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UnsubscribedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = UnsubscribedAtUtc.Value;
    }

    public void Reactivate(NotificationChannel preferredChannel)
    {
        PreferredChannel = preferredChannel;
        IsActive = true;
        UnsubscribedAtUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
