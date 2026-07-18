namespace Schoolera.Domain.Entities;

/// <summary>Append-only audit of school child age eligibility rule management actions.</summary>
public sealed class SchoolChildAgeEligibilityRuleAudit
{
    private SchoolChildAgeEligibilityRuleAudit()
    {
    }

    public SchoolChildAgeEligibilityRuleAudit(
        Guid schoolId,
        Guid? ruleId,
        string action,
        Guid actorUserId,
        string? metadata)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        RuleId = ruleId;
        Action = action.Trim();
        ActorUserId = actorUserId;
        Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid? RuleId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid ActorUserId { get; private set; }

    public string? Metadata { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public static class SchoolChildAgeEligibilityRuleAuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Published = "Published";
    public const string Unpublished = "Unpublished";
    public const string Deactivated = "Deactivated";
    public const string Cloned = "Cloned";
    public const string ScopeChanged = "ScopeChanged";
}
