using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Auth;

/// <summary>Central role → permission matrix for school-scoped memberships.</summary>
public static class SchoolPortalPermissionMatrix
{
    public static bool SupportsBranchScope(SchoolTeamRole role) =>
        SchoolTeamMember.SupportsBranchScope(role);

    public static bool RoleSupportsBranchScope(SchoolTeamRole role) => SupportsBranchScope(role);

    public static IReadOnlySet<SchoolPortalPermission> ForOwner() =>
        Enum.GetValues<SchoolPortalPermission>().ToHashSet();

    public static IReadOnlySet<SchoolPortalPermission> ForRole(SchoolTeamRole role) =>
        Enum.GetValues<SchoolPortalPermission>()
            .Where(permission => HasPermission(isOwner: false, role, permission))
            .ToHashSet();

    public static bool HasPermission(bool isOwner, SchoolTeamRole? role, SchoolPortalPermission permission)
    {
        if (isOwner)
        {
            return true;
        }

        return role switch
        {
            SchoolTeamRole.SchoolAdmin => permission is not SchoolPortalPermission.ManageTeam
                and not SchoolPortalPermission.TransferOwnership,
            SchoolTeamRole.AdmissionOfficer => permission is
                SchoolPortalPermission.ViewDashboard
                or SchoolPortalPermission.ViewApplications
                or SchoolPortalPermission.ManageApplicationReview
                or SchoolPortalPermission.DownloadApplicationAttachments
                or SchoolPortalPermission.ExportApplications
                or SchoolPortalPermission.ManageAdmissionRequirements
                or SchoolPortalPermission.ManageAdmissionQuestions,
            SchoolTeamRole.FinanceOfficer => permission is
                SchoolPortalPermission.ViewDashboard
                or SchoolPortalPermission.ViewFees
                or SchoolPortalPermission.ManageFees,
            SchoolTeamRole.ContentModerator => permission is
                SchoolPortalPermission.ViewDashboard
                or SchoolPortalPermission.ViewProfile
                or SchoolPortalPermission.ManageProfile
                or SchoolPortalPermission.ManageFacilities
                or SchoolPortalPermission.ManageGallery
                or SchoolPortalPermission.ManageServices
                or SchoolPortalPermission.ManagePublicContact
                or SchoolPortalPermission.ManageContent,
            _ => false,
        };
    }

    public static SchoolPortalPermissionsDto BuildPermissionsDto(
        bool isOwner,
        SchoolTeamRole? role,
        SchoolBranchScopeMode scopeMode,
        IReadOnlyList<Guid> allowedBranchIds)
    {
        bool P(SchoolPortalPermission permission) => HasPermission(isOwner, role, permission);

        return new SchoolPortalPermissionsDto(
            CanViewDashboard: P(SchoolPortalPermission.ViewDashboard),
            CanViewTeam: P(SchoolPortalPermission.ViewTeam),
            CanManageTeam: P(SchoolPortalPermission.ManageTeam),
            CanTransferOwnership: P(SchoolPortalPermission.TransferOwnership),
            CanViewProfile: P(SchoolPortalPermission.ViewProfile),
            CanManageProfile: P(SchoolPortalPermission.ManageProfile),
            CanManageBranches: P(SchoolPortalPermission.ManageBranches),
            CanManageOfferings: P(SchoolPortalPermission.ManageOfferings),
            CanManageFacilities: P(SchoolPortalPermission.ManageFacilities),
            CanManageGallery: P(SchoolPortalPermission.ManageGallery),
            CanManageServices: P(SchoolPortalPermission.ManageServices),
            CanManagePublicContact: P(SchoolPortalPermission.ManagePublicContact),
            CanViewApplications: P(SchoolPortalPermission.ViewApplications),
            CanManageApplicationReview: P(SchoolPortalPermission.ManageApplicationReview),
            CanDownloadApplicationAttachments: P(SchoolPortalPermission.DownloadApplicationAttachments),
            CanExportApplications: P(SchoolPortalPermission.ExportApplications),
            CanManageAdmissionRequirements: P(SchoolPortalPermission.ManageAdmissionRequirements),
            CanManageAdmissionQuestions: P(SchoolPortalPermission.ManageAdmissionQuestions),
            CanViewFees: P(SchoolPortalPermission.ViewFees),
            CanManageFees: P(SchoolPortalPermission.ManageFees),
            CanManageContent: P(SchoolPortalPermission.ManageContent),
            BranchScopeMode: isOwner || role is SchoolTeamRole.SchoolAdmin or SchoolTeamRole.ContentModerator
                ? SchoolBranchScopeMode.AllBranches
                : scopeMode,
            AllowedBranchIds: isOwner || role is SchoolTeamRole.SchoolAdmin or SchoolTeamRole.ContentModerator
                ? Array.Empty<Guid>()
                : allowedBranchIds);
    }
}
