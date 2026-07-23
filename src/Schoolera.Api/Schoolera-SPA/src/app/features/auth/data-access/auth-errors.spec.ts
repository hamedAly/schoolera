import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoService } from '@jsverse/transloco';
import { describe, expect, it, vi } from 'vitest';

import {
  mapAuthServerErrors,
  resolveAuthErrorKeys,
  resolveAuthFailureCodes,
  translateAuthErrorCodes,
} from './auth-errors';

function createTransloco(): TranslocoService {
  return {
    translate: (key: string) => key,
  } as unknown as TranslocoService;
}

describe('auth-errors mapper', () => {
  it('maps Identity-style password codes to Transloco keys', () => {
    expect(
      resolveAuthErrorKeys([
        'auth.passwordRequiresUppercase',
        'auth.passwordRequiresNonAlphanumeric',
      ]),
    ).toEqual([
      'auth.errors.passwordRequiresUppercase',
      'auth.errors.passwordRequiresNonAlphanumeric',
    ]);
  });

  it('maps emailAlreadyExists and legacy duplicateEmail to the same key', () => {
    expect(resolveAuthErrorKeys(['auth.emailAlreadyExists'])).toEqual([
      'auth.errors.emailAlreadyExists',
    ]);
    expect(resolveAuthErrorKeys(['auth.duplicateEmail'])).toEqual([
      'auth.errors.emailAlreadyExists',
    ]);
  });

  it('never uses localized error text for branching', () => {
    const transloco = createTransloco();
    const message = translateAuthErrorCodes(transloco, ['auth.passwordRequiresLowercase']);
    expect(message).toBe('auth.errors.passwordRequiresLowercase');
  });

  it('extracts errorCodes from HttpErrorResponse bodies', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        succeeded: false,
        errors: ['Passwords must have at least one uppercase.'],
        errorCodes: ['auth.passwordRequiresUppercase'],
      },
    });

    expect(resolveAuthFailureCodes(error)).toEqual(['auth.passwordRequiresUppercase']);
  });

  it('assigns password codes to the password field for summaries', () => {
    const mapped = mapAuthServerErrors(createTransloco(), [
      'auth.passwordRequiresDigit',
      'auth.emailAlreadyExists',
    ]);

    expect(mapped.fieldMessages['password']).toContain('auth.errors.passwordRequiresDigit');
    expect(mapped.fieldMessages['email']).toContain('auth.errors.emailAlreadyExists');
    expect(mapped.summaryItems.some((item) => item.fieldId === 'password')).toBe(true);
  });

  it('maps legal.privacy_required to the Privacy field message', () => {
    const mapped = mapAuthServerErrors(createTransloco(), ['legal.privacy_required']);
    expect(resolveAuthErrorKeys(['legal.privacy_required'])).toEqual(['auth.errors.privacyRequired']);
    expect(mapped.fieldMessages['privacyAccepted']).toContain('auth.errors.privacyRequired');
    expect(mapped.summaryItems[0]?.fieldId).toBe('privacyAccepted');
  });

  it('maps legal.terms_required to the Terms field message', () => {
    const mapped = mapAuthServerErrors(createTransloco(), ['legal.terms_required']);
    expect(mapped.fieldMessages['termsAccepted']).toContain('auth.errors.termsRequired');
  });
});
