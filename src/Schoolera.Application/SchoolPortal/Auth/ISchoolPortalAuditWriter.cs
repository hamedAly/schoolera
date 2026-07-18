namespace Schoolera.Application.SchoolPortal.Auth;

public static class SchoolPortalAuditActions
{
    public const string MemberAdded = "schoolPortal.team.member_added";
    public const string MemberActivated = "schoolPortal.team.member_activated";
    public const string MemberDeactivated = "schoolPortal.team.member_deactivated";
    public const string MemberUpdated = "schoolPortal.team.member_updated";
    public const string RoleChanged = "schoolPortal.team.role_changed";
    public const string BranchScopeChanged = "schoolPortal.team.branch_scope_changed";
    public const string OwnershipTransferred = "schoolPortal.team.ownership_transferred";
    public const string InterviewFaqCreated = "schoolPortal.interviewFaq.created";
    public const string InterviewFaqUpdated = "schoolPortal.interviewFaq.updated";
    public const string InterviewFaqPublished = "schoolPortal.interviewFaq.published";
    public const string InterviewFaqUnpublished = "schoolPortal.interviewFaq.unpublished";
    public const string InterviewFaqActivated = "schoolPortal.interviewFaq.activated";
    public const string InterviewFaqDeactivated = "schoolPortal.interviewFaq.deactivated";
    public const string InterviewFaqReordered = "schoolPortal.interviewFaq.reordered";
}

public interface ISchoolPortalAuditWriter
{
    Task WriteAsync(
        Guid actorUserId,
        string action,
        string entityType,
        string? entityId,
        string? summary,
        CancellationToken cancellationToken = default);
}
