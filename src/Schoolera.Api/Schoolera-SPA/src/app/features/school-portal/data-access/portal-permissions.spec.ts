import { describe, expect, it } from 'vitest';

import {
  branchScopeLabelKey,
  hasAnyPortalPermission,
  hasPortalPermission,
  isPortalNavAllowed,
  roleSupportsBranchScope,
  teamRoleLabelKey,
} from './portal-permissions';
import {
  SchoolBranchScopeMode,
  SchoolPortalPermissionsDto,
  SchoolTeamRole,
} from './school-portal-permissions.models';

const ownerPerms: SchoolPortalPermissionsDto = {
  canViewTeam: true,
  canManageTeam: true,
  canViewApplications: true,
  canViewFees: false,
};

describe('portal-permissions helpers', () => {
  it('hasPortalPermission reads boolean flags', () => {
    expect(hasPortalPermission(ownerPerms, 'canManageTeam')).toBe(true);
    expect(hasPortalPermission(ownerPerms, 'canViewFees')).toBe(false);
    expect(hasPortalPermission(null, 'canManageTeam')).toBe(false);
  });

  it('hasAnyPortalPermission requires at least one true flag', () => {
    expect(hasAnyPortalPermission(ownerPerms, ['canViewFees', 'canViewTeam'])).toBe(true);
    expect(hasAnyPortalPermission(ownerPerms, ['canViewFees', 'canManageFees'])).toBe(false);
  });

  it('isPortalNavAllowed allows all when permissions are missing (legacy)', () => {
    expect(isPortalNavAllowed(undefined, ['canManageTeam'])).toBe(true);
    expect(isPortalNavAllowed(null, ['canManageTeam'])).toBe(true);
    expect(isPortalNavAllowed(ownerPerms, ['canManageTeam'])).toBe(true);
    expect(isPortalNavAllowed(ownerPerms, ['canViewFees'])).toBe(false);
  });

  it('roleSupportsBranchScope only for admission and finance officers', () => {
    expect(roleSupportsBranchScope(SchoolTeamRole.AdmissionOfficer)).toBe(true);
    expect(roleSupportsBranchScope(SchoolTeamRole.FinanceOfficer)).toBe(true);
    expect(roleSupportsBranchScope(SchoolTeamRole.SchoolAdmin)).toBe(false);
    expect(roleSupportsBranchScope(SchoolTeamRole.ContentModerator)).toBe(false);
    expect(roleSupportsBranchScope(null)).toBe(false);
  });

  it('maps role and scope label keys', () => {
    expect(teamRoleLabelKey(SchoolTeamRole.SchoolAdmin)).toBe('portal.team.roles.schoolAdmin');
    expect(teamRoleLabelKey(undefined)).toBe('portal.team.roles.unknown');
    expect(branchScopeLabelKey(SchoolBranchScopeMode.SelectedBranches)).toBe(
      'portal.team.scope.selected',
    );
    expect(branchScopeLabelKey(SchoolBranchScopeMode.AllBranches)).toBe('portal.team.scope.all');
  });
});
