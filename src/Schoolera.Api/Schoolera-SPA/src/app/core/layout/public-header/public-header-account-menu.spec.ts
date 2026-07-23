import { describe, expect, it } from 'vitest';

import { AuthUser, SchooleraRoles } from '../../auth/auth.models';
import {
  accountAvatarInitials,
  accountDisplayName,
  buildPublicAccountMenuItems,
} from './public-header-account-menu';

function user(partial: Partial<AuthUser> & Pick<AuthUser, 'roles'>): AuthUser {
  return {
    id: 'u1',
    displayName: 'نور أحمد',
    email: 'parent@example.invalid',
    phoneNumber: null,
    accountStatus: 'Active',
    preferredLanguage: 'ar',
    postLoginDestination: '/parent',
    ...partial,
  };
}

describe('buildPublicAccountMenuItems', () => {
  it('returns empty list for anonymous users', () => {
    expect(buildPublicAccountMenuItems(null)).toEqual([]);
  });

  it('builds Parent account links ending with logout', () => {
    const items = buildPublicAccountMenuItems(user({ roles: [SchooleraRoles.Parent] }));
    expect(items.map((item) => item.id)).toEqual([
      'parent-dashboard',
      'parent-profile',
      'parent-children',
      'parent-applications',
      'parent-notifications',
      'logout',
    ]);
    expect(items.find((item) => item.id === 'parent-dashboard')?.route).toBe('/parent/dashboard');
    expect(items.at(-1)?.action).toBe('logout');
  });

  it('uses school portal destination for school roles', () => {
    const items = buildPublicAccountMenuItems(
      user({
        roles: [SchooleraRoles.SchoolOwner],
        postLoginDestination: '/school',
        displayName: 'Owner',
      }),
    );
    expect(items.map((item) => item.id)).toEqual(['school-portal', 'logout']);
    expect(items[0]?.route).toBe('/school');
  });

  it('uses admin dashboard for PlatformAdmin', () => {
    const items = buildPublicAccountMenuItems(
      user({
        roles: [SchooleraRoles.PlatformAdmin],
        postLoginDestination: '/admin',
      }),
    );
    expect(items.map((item) => item.id)).toEqual(['admin-dashboard', 'logout']);
    expect(items[0]?.route).toBe('/admin/dashboard');
  });
});

describe('account display helpers', () => {
  it('prefers display name and builds initials', () => {
    expect(accountDisplayName(user({ roles: [SchooleraRoles.Parent] }))).toBe('نور أحمد');
    expect(accountAvatarInitials('Nora Ahmed')).toBe('NA');
  });

  it('falls back to email when display name is blank', () => {
    expect(
      accountDisplayName(
        user({ roles: [SchooleraRoles.Parent], displayName: '  ', email: 'a@b.invalid' }),
      ),
    ).toBe('a@b.invalid');
  });
});
