import { describe, expect, it } from 'vitest';

import { maskEmail } from './mask-email';

describe('maskEmail', () => {
  it('masks the local part', () => {
    expect(maskEmail('mohamed@gmail.com')).toBe('m***@gmail.com');
  });

  it('returns the input when the email is malformed', () => {
    expect(maskEmail('not-an-email')).toBe('not-an-email');
  });
});
