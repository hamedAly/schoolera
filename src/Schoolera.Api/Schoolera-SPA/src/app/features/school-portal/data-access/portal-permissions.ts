import {
  PortalPermissionKey,
  SchoolBranchScopeMode,
  SchoolPortalPermissionsDto,
  SchoolTeamRole,
} from './school-portal-permissions.models';

export function hasPortalPermission(
  permissions: SchoolPortalPermissionsDto | null | undefined,
  flag: PortalPermissionKey,
): boolean {
  if (!permissions) {
    return false;
  }
  return !!permissions[flag];
}

export function hasAnyPortalPermission(
  permissions: SchoolPortalPermissionsDto | null | undefined,
  flags: readonly PortalPermissionKey[],
): boolean {
  return flags.some((flag) => hasPortalPermission(permissions, flag));
}

/** When backend has not yet attached permissions, treat as unrestricted (legacy). */
export function isPortalNavAllowed(
  permissions: SchoolPortalPermissionsDto | null | undefined,
  flags: readonly PortalPermissionKey[],
): boolean {
  if (!flags.length) {
    return true;
  }
  if (permissions == null) {
    return true;
  }
  return hasAnyPortalPermission(permissions, flags);
}

export function roleSupportsBranchScope(role: SchoolTeamRole | null | undefined): boolean {
  return role === SchoolTeamRole.AdmissionOfficer || role === SchoolTeamRole.FinanceOfficer;
}

export function teamRoleLabelKey(role: SchoolTeamRole | null | undefined): string {
  switch (role) {
    case SchoolTeamRole.SchoolAdmin:
      return 'portal.team.roles.schoolAdmin';
    case SchoolTeamRole.AdmissionOfficer:
      return 'portal.team.roles.admissionOfficer';
    case SchoolTeamRole.FinanceOfficer:
      return 'portal.team.roles.financeOfficer';
    case SchoolTeamRole.ContentModerator:
      return 'portal.team.roles.contentModerator';
    default:
      return 'portal.team.roles.unknown';
  }
}

export function branchScopeLabelKey(mode: SchoolBranchScopeMode | null | undefined): string {
  switch (mode) {
    case SchoolBranchScopeMode.SelectedBranches:
      return 'portal.team.scope.selected';
    case SchoolBranchScopeMode.AllBranches:
    default:
      return 'portal.team.scope.all';
  }
}
