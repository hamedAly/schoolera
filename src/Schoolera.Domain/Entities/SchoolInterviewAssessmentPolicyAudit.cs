namespace Schoolera.Domain.Entities;

/// <summary>Append-only audit of school interview/assessment policy management actions.</summary>
public sealed class SchoolInterviewAssessmentPolicyAudit
{
    private SchoolInterviewAssessmentPolicyAudit()
    {
    }

    public SchoolInterviewAssessmentPolicyAudit(
        Guid schoolId,
        Guid? policyId,
        string action,
        Guid actorUserId,
        string? metadata)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        PolicyId = policyId;
        Action = action.Trim();
        ActorUserId = actorUserId;
        Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid? PolicyId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid ActorUserId { get; private set; }

    /// <summary>Safe non-sensitive metadata. Never credentials or meeting URLs.</summary>
    public string? Metadata { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public static class SchoolInterviewAssessmentPolicyAuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Published = "Published";
    public const string Unpublished = "Unpublished";
    public const string Deactivated = "Deactivated";
    public const string Cloned = "Cloned";
    public const string ScopeChanged = "ScopeChanged";
}
