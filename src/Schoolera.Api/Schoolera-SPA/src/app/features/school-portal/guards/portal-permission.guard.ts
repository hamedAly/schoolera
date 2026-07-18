import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { PortalContextService } from '../data-access/portal-context.service';
import { hasAnyPortalPermission, hasPortalPermission } from '../data-access/portal-permissions';
import { PortalPermissionKey } from '../data-access/school-portal-permissions.models';

/**
 * Reads `data.portalPermission` (single flag) or `data.portalPermissions` (any-of).
 * Redirects to school overview when the current school permissions deny access.
 * When permissions are not yet attached (legacy), navigation is allowed.
 */
export const portalPermissionGuard: CanActivateFn = (route) => {
  const context = inject(PortalContextService);
  const router = inject(Router);

  const single = route.data['portalPermission'] as PortalPermissionKey | undefined;
  const anyOf = route.data['portalPermissions'] as readonly PortalPermissionKey[] | undefined;
  const flags: readonly PortalPermissionKey[] = anyOf?.length
    ? anyOf
    : single
      ? [single]
      : [];

  if (!flags.length) {
    return true;
  }

  const permissions = context.currentPermissions();
  if (permissions == null) {
    return true;
  }

  const allowed = flags.length === 1
    ? hasPortalPermission(permissions, flags[0])
    : hasAnyPortalPermission(permissions, flags);

  if (allowed) {
    return true;
  }

  const schoolId =
    route.paramMap.get('schoolId') ??
    route.parent?.paramMap.get('schoolId') ??
    context.currentSchoolId();

  if (schoolId) {
    return router.createUrlTree(['/school', schoolId, 'overview']);
  }

  return router.createUrlTree(['/unauthorized']);
};
