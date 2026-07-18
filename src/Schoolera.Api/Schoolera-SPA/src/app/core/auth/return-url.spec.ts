import { describe, expect, it } from 'vitest';

import { sanitizeReturnUrl } from './return-url';

describe('sanitizeReturnUrl', () => {
  it('allows internal paths', () => {
    expect(sanitizeReturnUrl('/schools')).toBe('/schools');
    expect(sanitizeReturnUrl('/parent?tab=home')).toBe('/parent?tab=home');
  });

  it('rejects external and protocol-relative urls', () => {
    expect(sanitizeReturnUrl('https://evil.test')).toBe('/');
    expect(sanitizeReturnUrl('//evil.test')).toBe('/');
  });

  it('blocks auth loop targets', () => {
    expect(sanitizeReturnUrl('/auth/login')).toBe('/');
    expect(sanitizeReturnUrl('/auth/register/parent')).toBe('/');
  });
});
