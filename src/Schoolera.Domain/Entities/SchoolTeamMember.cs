using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Active or deactivated school-scoped team membership. Ownership remains on
/// <see cref="School.OwnerUserId"/> and is never duplicated here.
/// </summary>
public sealed class SchoolTeamMember
{
    private SchoolTeamMember()
    {
        BranchAssignments = new List<SchoolTeamMemberBranch>();
    }

    public SchoolTeamMember(
        Guid schoolId,
        Guid userId,
        SchoolTeamRole role,
        Guid createdByUserId,
        SchoolBranchScopeMode branchScopeMode = SchoolBranchScopeMode.AllBranches)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        if (!Enum.IsDefined(branchScopeMode))
        {
            throw new ArgumentOutOfRangeException(nameof(branchScopeMode));
        }

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        UserId = userId;
        Role = role;
        BranchScopeMode = SupportsBranchScope(role)
            ? branchScopeMode
            : SchoolBranchScopeMode.AllBranches;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        CreatedByUserId = createdByUserId;
        UpdatedAtUtc = CreatedAtUtc;
        BranchAssignments = new List<SchoolTeamMemberBranch>();
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public SchoolTeamRole Role { get; private set; }

    public SchoolBranchScopeMode BranchScopeMode { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    /// <summary>Explicit branch allowlist when <see cref="BranchScopeMode"/> is SelectedBranches.</summary>
    public ICollection<SchoolTeamMemberBranch> BranchAssignments { get; private set; }

    /// <summary>True when this membership role may use optional branch scope.</summary>
    public bool UsesBranchScope => SupportsBranchScope(Role);

    public static bool SupportsBranchScope(SchoolTeamRole role) =>
        role is SchoolTeamRole.AdmissionOfficer or SchoolTeamRole.FinanceOfficer;

    public bool AllowsBranch(Guid branchId)
    {
        if (!UsesBranchScope || BranchScopeMode == SchoolBranchScopeMode.AllBranches)
        {
            return true;
        }

        return BranchAssignments.Any(entry => entry.SchoolBranchId == branchId);
    }

    public void ChangeRole(SchoolTeamRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        Role = role;
        if (!SupportsBranchScope(role))
        {
            BranchScopeMode = SchoolBranchScopeMode.AllBranches;
            BranchAssignments.Clear();
        }

        Touch();
    }

    /// <summary>
    /// Replaces branch scope. SelectedBranches requires at least one branch id.
    /// Roles that do not support branch scope ignore the request and stay AllBranches.
    /// </summary>
    public void SetBranchScope(SchoolBranchScopeMode mode, IEnumerable<Guid>? branchIds)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (!UsesBranchScope)
        {
            BranchScopeMode = SchoolBranchScopeMode.AllBranches;
            BranchAssignments.Clear();
            Touch();
            return;
        }

        if (mode == SchoolBranchScopeMode.SelectedBranches)
        {
            var distinct = (branchIds ?? Array.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            if (distinct.Length == 0)
            {
                throw new InvalidOperationException(
                    "SelectedBranches scope requires at least one branch id.");
            }

            BranchScopeMode = SchoolBranchScopeMode.SelectedBranches;
            BranchAssignments.Clear();
            foreach (var branchId in distinct)
            {
                BranchAssignments.Add(new SchoolTeamMemberBranch(Id, branchId));
            }
        }
        else
        {
            BranchScopeMode = SchoolBranchScopeMode.AllBranches;
            BranchAssignments.Clear();
        }

        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DeactivatedAtUtc.Value;
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        DeactivatedAtUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
