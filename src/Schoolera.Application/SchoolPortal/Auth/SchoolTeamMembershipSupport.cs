using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace Schoolera.Application.SchoolPortal.Auth;

public static class SchoolTeamMembershipSupport
{
    public static Result<(SchoolBranchScopeMode Mode, IReadOnlyList<Guid> BranchIds)> NormalizeScope(
        SchoolTeamRole role,
        SchoolBranchScopeMode mode,
        IReadOnlyList<Guid>? branchIds,
        IStringLocalizer<SchoolPortalMessages> localizer)
    {
        if (!SchoolPortalPermissionMatrix.SupportsBranchScope(role))
        {
            return Result<(SchoolBranchScopeMode, IReadOnlyList<Guid>)>.Success(
                (SchoolBranchScopeMode.AllBranches, Array.Empty<Guid>()));
        }

        if (!Enum.IsDefined(mode))
        {
            return SchoolPortalResults.FailureForCode<(SchoolBranchScopeMode, IReadOnlyList<Guid>)>(
                localizer, SchoolPortalErrorCodes.InvalidBranchScope);
        }

        if (mode == SchoolBranchScopeMode.AllBranches)
        {
            return Result<(SchoolBranchScopeMode, IReadOnlyList<Guid>)>.Success(
                (SchoolBranchScopeMode.AllBranches, Array.Empty<Guid>()));
        }

        var distinct = (branchIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinct.Length == 0)
        {
            return SchoolPortalResults.FailureForCode<(SchoolBranchScopeMode, IReadOnlyList<Guid>)>(
                localizer, SchoolPortalErrorCodes.InvalidBranchScope);
        }

        return Result<(SchoolBranchScopeMode, IReadOnlyList<Guid>)>.Success(
            (SchoolBranchScopeMode.SelectedBranches, distinct));
    }

    public static bool IsEligibleForRole(IReadOnlyList<string> identityRoles, SchoolTeamRole role)
    {
        if (identityRoles.Contains(SchooleraRoles.Parent) ||
            identityRoles.Contains(SchooleraRoles.PlatformAdmin) ||
            identityRoles.Contains(SchooleraRoles.SupportAgent))
        {
            return false;
        }

        return role switch
        {
            SchoolTeamRole.SchoolAdmin => identityRoles.Contains(SchooleraRoles.SchoolAdmin),
            SchoolTeamRole.AdmissionOfficer => identityRoles.Contains(SchooleraRoles.AdmissionOfficer),
            SchoolTeamRole.FinanceOfficer => identityRoles.Contains(SchooleraRoles.FinanceOfficer),
            SchoolTeamRole.ContentModerator => identityRoles.Contains(SchooleraRoles.ContentModerator),
            _ => false,
        };
    }

    public static async Task<Result<bool>> ApplyRoleAndScopeAsync(
        SchoolTeamMember member,
        SchoolTeamRole role,
        SchoolBranchScopeMode mode,
        IReadOnlyList<Guid> branchIds,
        ISchoolPortalRepository repository,
        IStringLocalizer<SchoolPortalMessages> localizer,
        CancellationToken cancellationToken)
    {
        if (mode == SchoolBranchScopeMode.SelectedBranches)
        {
            var schoolBranches = await repository.ListBranchesAsync(member.SchoolId, cancellationToken);
            var schoolBranchIds = schoolBranches.Select(b => b.Id).ToHashSet();
            if (branchIds.Any(id => !schoolBranchIds.Contains(id)))
            {
                return SchoolPortalResults.FailureForCode<bool>(
                    localizer, SchoolPortalErrorCodes.InvalidBranchScope);
            }
        }

        member.ChangeRole(role);
        member.SetBranchScope(mode, branchIds);
        return Result<bool>.Success(true);
    }
}
