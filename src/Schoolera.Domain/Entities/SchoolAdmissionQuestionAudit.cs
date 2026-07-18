namespace Schoolera.Domain.Entities;

public sealed class SchoolAdmissionQuestionAudit
{
    private SchoolAdmissionQuestionAudit()
    {
    }

    public SchoolAdmissionQuestionAudit(
        Guid schoolId,
        Guid? questionId,
        string action,
        Guid actorUserId,
        string? metadata = null)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        QuestionId = questionId;
        Action = action.Trim();
        ActorUserId = actorUserId;
        Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid? QuestionId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public string? Metadata { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public static class SchoolAdmissionQuestionAuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Reordered = "Reordered";
    public const string Published = "Published";
    public const string Unpublished = "Unpublished";
    public const string Deactivated = "Deactivated";
}
