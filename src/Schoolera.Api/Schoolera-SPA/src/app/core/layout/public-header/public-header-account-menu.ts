import {
  AuthUser,
  hasAnyRole,
  SchooleraRoles,
  SchoolPortalEntryRoles,
} from '../../auth/auth.models';

export interface PublicAccountMenuItem {
  readonly id: string;
  readonly labelKey: string;
  readonly route?: string;
  readonly action?: 'logout';
}

/** Builds role-aware account menu entries for the public header. */
export function buildPublicAccountMenuItems(user: AuthUser | null): PublicAccountMenuItem[] {
  if (!user) {
    return [];
  }

  const items: PublicAccountMenuItem[] = [];

  if (hasAnyRole(user, [SchooleraRoles.Parent])) {
    items.push(
      {
        id: 'parent-dashboard',
        labelKey: 'nav.account.parentDashboard',
        route: '/parent/dashboard',
      },
      {
        id: 'parent-profile',
        labelKey: 'nav.account.profile',
        route: '/parent/profile',
      },
      {
        id: 'parent-children',
        labelKey: 'nav.account.children',
        route: '/parent/children',
      },
      {
        id: 'parent-applications',
        labelKey: 'nav.account.applications',
        route: '/parent/applications',
      },
      {
        id: 'parent-notifications',
        labelKey: 'nav.account.notifications',
        route: '/parent/notifications',
      },
    );
  }

  if (hasAnyRole(user, SchoolPortalEntryRoles)) {
    items.push({
      id: 'school-portal',
      labelKey: 'nav.account.schoolPortal',
      route: '/school',
    });
  }

  if (hasAnyRole(user, [SchooleraRoles.PlatformAdmin])) {
    items.push({
      id: 'admin-dashboard',
      labelKey: 'nav.account.adminDashboard',
      route: '/admin/dashboard',
    });
  } else if (hasAnyRole(user, [SchooleraRoles.SupportAgent])) {
    items.push({
      id: 'support',
      labelKey: 'nav.account.support',
      route: '/support',
    });
  }

  if (items.length === 0) {
    const destination = user.postLoginDestination?.trim() || '/';
    items.push({
      id: 'dashboard',
      labelKey: 'nav.account.dashboard',
      route: destination,
    });
  }

  items.push({
    id: 'logout',
    labelKey: 'nav.account.logout',
    action: 'logout',
  });

  return items;
}

export function accountDisplayName(user: AuthUser | null): string {
  if (!user) {
    return '';
  }

  const name = user.displayName?.trim();
  if (name) {
    return name;
  }

  return user.email?.trim() || '';
}

export function accountAvatarInitials(displayName: string): string {
  const parts = displayName
    .split(/\s+/)
    .map((part) => part.trim())
    .filter(Boolean);

  if (parts.length === 0) {
    return '?';
  }

  if (parts.length === 1) {
    return parts[0].slice(0, 2).toLocaleUpperCase();
  }

  return `${parts[0][0] ?? ''}${parts[1][0] ?? ''}`.toLocaleUpperCase();
}
