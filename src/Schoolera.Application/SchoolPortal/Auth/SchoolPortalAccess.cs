using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace Schoolera.Application.SchoolPortal.Auth;

public sealed record SchoolPortalAccessContext(
    Guid SchoolId,
    Guid UserId,
    bool IsOwner,
    SchoolTeamRole? MembershipRole,
    SchoolBranchScopeMode BranchScopeMode,
    IReadOnlyList<Guid> AllowedBranchIds,
    SchoolStatus SchoolStatus,
    bool IsEditable,
    SchoolPortalPermissionsDto PermissionsDto)
{
    public bool CanManageTeam => IsOwner;

    public bool AllowsAllBranches =>
        IsOwner ||
        MembershipRole is SchoolTeamRole.SchoolAdmin or SchoolTeamRole.ContentModerator ||
        BranchScopeMode == SchoolBranchScopeMode.AllBranches;

    /// <summary>Stable history actor role string for admission audit rows.</summary>
    public string HistoryActorRole =>
        IsOwner
            ? SchooleraRoles.SchoolOwner
            : MembershipRole switch
            {
                SchoolTeamRole.SchoolAdmin => SchooleraRoles.SchoolAdmin,
                SchoolTeamRole.AdmissionOfficer => SchooleraRoles.AdmissionOfficer,
                SchoolTeamRole.FinanceOfficer => SchooleraRoles.FinanceOfficer,
                SchoolTeamRole.ContentModerator => SchooleraRoles.ContentModerator,
                _ => SchooleraRoles.SchoolAdmin,
            };

    public bool HasPermission(SchoolPortalPermission permission) =>
        SchoolPortalPermissionMatrix.HasPermission(IsOwner, MembershipRole, permission);

    public bool CanAccessBranch(Guid branchId)
    {
        if (AllowsAllBranches)
        {
            return true;
        }

        return AllowedBranchIds.Contains(branchId);
    }
}

public interface ISchoolPortalAccess
{
    Task<Result<SchoolPortalAccessContext>> ResolveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);
}

public sealed class SchoolPortalAccess(
    ISchoolPortalRepository repository,
    ICurrentUser currentUser,
    IStringLocalizer<SchoolPortalMessages> localizer) : ISchoolPortalAccess
{
    public async Task<Result<SchoolPortalAccessContext>> ResolveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser is not { IsAuthenticated: true, UserId: { } userId })
        {
            return SchoolPortalResults.FailureForCode<SchoolPortalAccessContext>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        var snapshot = await repository.GetAccessSnapshotAsync(schoolId, userId, cancellationToken);
        if (snapshot is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolPortalAccessContext>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        var isOwner = snapshot.OwnerUserId == userId;
        if (!isOwner && !snapshot.HasActiveMembership)
        {
            return SchoolPortalResults.FailureForCode<SchoolPortalAccessContext>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        var role = isOwner ? null : snapshot.MembershipRole;
        var scopeMode = isOwner
            ? SchoolBranchScopeMode.AllBranches
            : snapshot.BranchScopeMode;
        var allowedBranches = isOwner
            ? Array.Empty<Guid>()
            : snapshot.AllowedBranchIds;

        var permissions = SchoolPortalPermissionMatrix.BuildPermissionsDto(
            isOwner,
            role,
            scopeMode,
            allowedBranches);

        var isEditable = snapshot.Status is not SchoolStatus.Suspended;

        return Result<SchoolPortalAccessContext>.Success(new SchoolPortalAccessContext(
            schoolId,
            userId,
            isOwner,
            role,
            scopeMode,
            allowedBranches,
            snapshot.Status,
            isEditable,
            permissions));
    }

    public static Result<T> RequirePermission<T>(
        SchoolPortalAccessContext access,
        SchoolPortalPermission permission,
        IStringLocalizer<SchoolPortalMessages> localizer)
    {
        if (!access.HasPermission(permission))
        {
            return SchoolPortalResults.FailureForCode<T>(localizer, SchoolPortalErrorCodes.AccessDenied);
        }

        return Result<T>.Success(default!);
    }

    public static Result<T> RequireEditablePermission<T>(
        SchoolPortalAccessContext access,
        SchoolPortalPermission permission,
        IStringLocalizer<SchoolPortalMessages> localizer)
    {
        if (!access.IsEditable)
        {
            return SchoolPortalResults.FailureForCode<T>(localizer, SchoolPortalErrorCodes.NotEditable);
        }

        return RequirePermission<T>(access, permission, localizer);
    }

    public static Result<T> RequireOwner<T>(
        SchoolPortalAccessContext access,
        IStringLocalizer<SchoolPortalMessages> localizer)
    {
        if (!access.IsOwner)
        {
            return SchoolPortalResults.FailureForCode<T>(localizer, SchoolPortalErrorCodes.OwnerRequired);
        }

        return Result<T>.Success(default!);
    }

    public static Result<T> RequireBranch<T>(
        SchoolPortalAccessContext access,
        Guid branchId,
        IStringLocalizer<SchoolPortalMessages> localizer)
    {
        if (!access.CanAccessBranch(branchId))
        {
            return SchoolPortalResults.FailureForCode<T>(localizer, SchoolPortalErrorCodes.BranchOutOfScope);
        }

        return Result<T>.Success(default!);
    }
}

/// <summary>Legacy alias so older Common usings keep compiling during migration.</summary>
public static class SchoolPortalAccessLegacy
{
}
