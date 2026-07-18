using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

/// <summary>
/// Unit coverage for Prompt 8 school-portal permission matrix and branch-scope domain rules.
/// </summary>
public sealed class SchoolPortalPermissionMatrixTests
{
    [Fact]
    public void ForOwner_IncludesManageTeamAndTransferOwnership()
    {
        var permissions = SchoolPortalPermissionMatrix.ForOwner();

        Assert.Contains(SchoolPortalPermission.ManageTeam, permissions);
        Assert.Contains(SchoolPortalPermission.TransferOwnership, permissions);
        Assert.Contains(SchoolPortalPermission.ViewTeam, permissions);
        Assert.Contains(SchoolPortalPermission.ViewApplications, permissions);
        Assert.Contains(SchoolPortalPermission.ManageFees, permissions);
        Assert.Contains(SchoolPortalPermission.ManageContent, permissions);
    }

    [Fact]
    public void ForRole_SchoolAdmin_HasEditCapabilitiesButNotManageTeamOrTransfer()
    {
        var permissions = SchoolPortalPermissionMatrix.ForRole(SchoolTeamRole.SchoolAdmin);

        Assert.DoesNotContain(SchoolPortalPermission.ManageTeam, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.TransferOwnership, permissions);
        Assert.Contains(SchoolPortalPermission.ViewTeam, permissions);
        Assert.Contains(SchoolPortalPermission.ManageProfile, permissions);
        Assert.Contains(SchoolPortalPermission.ViewApplications, permissions);
        Assert.Contains(SchoolPortalPermission.ManageFees, permissions);
        Assert.Contains(SchoolPortalPermission.ManageContent, permissions);
    }

    [Fact]
    public void ForRole_AdmissionOfficer_HasViewApplicationsNotManageFees()
    {
        var permissions = SchoolPortalPermissionMatrix.ForRole(SchoolTeamRole.AdmissionOfficer);

        Assert.Contains(SchoolPortalPermission.ViewApplications, permissions);
        Assert.Contains(SchoolPortalPermission.ManageApplicationReview, permissions);
        Assert.Contains(SchoolPortalPermission.DownloadApplicationAttachments, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ManageFees, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ViewFees, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ViewTeam, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ManageTeam, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ManageProfile, permissions);
    }

    [Fact]
    public void ForRole_FinanceOfficer_HasManageFeesNotViewApplications()
    {
        var permissions = SchoolPortalPermissionMatrix.ForRole(SchoolTeamRole.FinanceOfficer);

        Assert.Contains(SchoolPortalPermission.ViewFees, permissions);
        Assert.Contains(SchoolPortalPermission.ManageFees, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ViewApplications, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ManageApplicationReview, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ManageTeam, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ManageContent, permissions);
    }

    [Fact]
    public void ForRole_ContentModerator_HasManageContentNotViewApplications()
    {
        var permissions = SchoolPortalPermissionMatrix.ForRole(SchoolTeamRole.ContentModerator);

        Assert.Contains(SchoolPortalPermission.ManageContent, permissions);
        Assert.Contains(SchoolPortalPermission.ViewProfile, permissions);
        Assert.Contains(SchoolPortalPermission.ManageProfile, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ViewApplications, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ViewFees, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ViewTeam, permissions);
        Assert.DoesNotContain(SchoolPortalPermission.ManageTeam, permissions);
    }

    [Theory]
    [InlineData(SchoolTeamRole.AdmissionOfficer, true)]
    [InlineData(SchoolTeamRole.FinanceOfficer, true)]
    [InlineData(SchoolTeamRole.SchoolAdmin, false)]
    [InlineData(SchoolTeamRole.ContentModerator, false)]
    public void RoleSupportsBranchScope_MatchesPrompt8Roles(SchoolTeamRole role, bool expected)
    {
        Assert.Equal(expected, SchoolPortalPermissionMatrix.RoleSupportsBranchScope(role));
    }

    [Fact]
    public void SchoolTeamMember_AdmissionOfficer_UsesBranchScope_AllowsAllByDefault()
    {
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.AdmissionOfficer,
            Guid.NewGuid());

        Assert.True(member.UsesBranchScope);
        Assert.Equal(SchoolBranchScopeMode.AllBranches, member.BranchScopeMode);
        Assert.True(member.AllowsBranch(Guid.NewGuid()));
    }

    [Fact]
    public void SchoolTeamMember_SetBranchScope_SelectedBranches_RequiresAtLeastOneId()
    {
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.FinanceOfficer,
            Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            member.SetBranchScope(SchoolBranchScopeMode.SelectedBranches, Array.Empty<Guid>()));
    }

    [Fact]
    public void SchoolTeamMember_SetBranchScope_SelectedBranches_RestrictsAllowlist()
    {
        var allowed = Guid.NewGuid();
        var denied = Guid.NewGuid();
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.AdmissionOfficer,
            Guid.NewGuid());

        member.SetBranchScope(SchoolBranchScopeMode.SelectedBranches, [allowed]);

        Assert.Equal(SchoolBranchScopeMode.SelectedBranches, member.BranchScopeMode);
        Assert.True(member.AllowsBranch(allowed));
        Assert.False(member.AllowsBranch(denied));
        Assert.Single(member.BranchAssignments);
        Assert.Equal(allowed, member.BranchAssignments.Single().SchoolBranchId);
    }

    [Fact]
    public void SchoolTeamMember_ChangeRole_ToContentModerator_ClearsBranchAssignments()
    {
        var branchId = Guid.NewGuid();
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.AdmissionOfficer,
            Guid.NewGuid());
        member.SetBranchScope(SchoolBranchScopeMode.SelectedBranches, [branchId]);

        member.ChangeRole(SchoolTeamRole.ContentModerator);

        Assert.False(member.UsesBranchScope);
        Assert.Equal(SchoolBranchScopeMode.AllBranches, member.BranchScopeMode);
        Assert.Empty(member.BranchAssignments);
        Assert.True(member.AllowsBranch(Guid.NewGuid()));
    }

    [Fact]
    public void SchoolTeamMember_SchoolAdmin_SetBranchScope_IsIgnored()
    {
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.SchoolAdmin,
            Guid.NewGuid());

        member.SetBranchScope(SchoolBranchScopeMode.SelectedBranches, [Guid.NewGuid()]);

        Assert.False(member.UsesBranchScope);
        Assert.Equal(SchoolBranchScopeMode.AllBranches, member.BranchScopeMode);
        Assert.Empty(member.BranchAssignments);
    }
}
