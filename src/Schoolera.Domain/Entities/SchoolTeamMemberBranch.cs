namespace Schoolera.Domain.Entities;

/// <summary>Explicit branch allowlist entry for a school team membership.</summary>
public sealed class SchoolTeamMemberBranch
{
    private SchoolTeamMemberBranch()
    {
    }

    public SchoolTeamMemberBranch(Guid schoolTeamMemberId, Guid schoolBranchId)
    {
        SchoolTeamMemberId = schoolTeamMemberId;
        SchoolBranchId = schoolBranchId;
    }

    public Guid SchoolTeamMemberId { get; private set; }

    public SchoolTeamMember SchoolTeamMember { get; private set; } = null!;

    public Guid SchoolBranchId { get; private set; }

    public SchoolBranch SchoolBranch { get; private set; } = null!;
}
