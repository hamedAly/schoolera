using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Append-only admission timeline entry. Internal notes are never Parent-visible.</summary>
public sealed class AdmissionApplicationHistory
{
    private AdmissionApplicationHistory()
    {
    }

    public AdmissionApplicationHistory(
        Guid admissionApplicationId,
        AdmissionApplicationStatus? fromStatus,
        AdmissionApplicationStatus toStatus,
        string action,
        Guid actorUserId,
        string actorRole,
        bool parentVisible,
        string? parentVisibleNote,
        string? internalNote)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Action = action;
        ActorUserId = actorUserId;
        ActorRole = actorRole;
        ParentVisible = parentVisible;
        ParentVisibleNote = string.IsNullOrWhiteSpace(parentVisibleNote) ? null : parentVisibleNote.Trim();
        InternalNote = string.IsNullOrWhiteSpace(internalNote) ? null : internalNote.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public AdmissionApplication AdmissionApplication { get; private set; } = null!;

    public AdmissionApplicationStatus? FromStatus { get; private set; }

    public AdmissionApplicationStatus ToStatus { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid ActorUserId { get; private set; }

    public string ActorRole { get; private set; } = string.Empty;

    public bool ParentVisible { get; private set; }

    public string? ParentVisibleNote { get; private set; }

    public string? InternalNote { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
