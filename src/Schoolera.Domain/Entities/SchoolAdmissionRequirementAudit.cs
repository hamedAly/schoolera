namespace Schoolera.Domain.Entities;

/// <summary>Append-only audit of school admission-requirement management actions (no sensitive payloads).</summary>
public sealed class SchoolAdmissionRequirementAudit
{
    private SchoolAdmissionRequirementAudit()
    {
    }

    public SchoolAdmissionRequirementAudit(
        Guid schoolId,
        Guid? requirementId,
        string action,
        Guid actorUserId,
        string? requirementCode,
        string? metadata)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        RequirementId = requirementId;
        Action = action.Trim();
        ActorUserId = actorUserId;
        RequirementCode = string.IsNullOrWhiteSpace(requirementCode) ? null : requirementCode.Trim().ToLowerInvariant();
        Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid? RequirementId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid ActorUserId { get; private set; }

    public string? RequirementCode { get; private set; }

    /// <summary>Safe non-sensitive metadata (e.g. sort counts). Never file contents or PII values.</summary>
    public string? Metadata { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public static class SchoolAdmissionRequirementAuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Reordered = "Reordered";
    public const string Published = "Published";
    public const string Unpublished = "Unpublished";
    public const string Deactivated = "Deactivated";
}
