import { describe, expect, it } from 'vitest';

import {
  CHILD_PROFILE_FIELD_CODES,
  PARENT_PROFILE_FIELD_CODES,
  profileFieldEditLink,
} from './admission-requirements.models';

describe('admission-requirements.models', () => {
  it('profileFieldEditLink routes parent fields to profile', () => {
    expect(PARENT_PROFILE_FIELD_CODES.has(10)).toBe(true);
    const link = profileFieldEditLink(10, 'child-1', '/return');
    expect(link?.route).toEqual(['/parent/profile']);
    expect(link?.queryParams.returnUrl).toBe('/return');
  });

  it('profileFieldEditLink routes child fields to child edit', () => {
    expect(CHILD_PROFILE_FIELD_CODES.has(30)).toBe(true);
    const link = profileFieldEditLink(30, 'child-1', '/return');
    expect(link?.route).toEqual(['/parent/children', 'child-1', 'edit']);
  });
});
