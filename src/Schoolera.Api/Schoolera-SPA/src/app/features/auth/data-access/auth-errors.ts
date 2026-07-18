import { TranslocoService } from '@jsverse/transloco';

import { FormErrorSummaryItem } from '../../../shared/ui/form-error-summary/form-error-summary';
import { extractApiFailure } from '../../../core/http/extract-api-failure';

/** Maps stable API `errorCodes` → Transloco keys. Never parse localized `errors` text. */
const AUTH_ERROR_CODE_TO_KEY: Record<string, string> = {
  'auth.invalidCredentials': 'auth.errors.invalidCredentials',
  'auth.accountSuspended': 'auth.errors.accountSuspended',
  'auth.accountNotVerified': 'auth.errors.accountNotVerified',
  'auth.emailAlreadyExists': 'auth.errors.emailAlreadyExists',
  'auth.duplicateEmail': 'auth.errors.emailAlreadyExists',
  'auth.duplicatePhone': 'auth.errors.duplicatePhone',
  'auth.invalidEmail': 'auth.errors.invalidEmail',
  'auth.invalidVerificationCode': 'auth.errors.invalidVerificationCode',
  'auth.expiredVerificationCode': 'auth.errors.expiredVerificationCode',
  'auth.codeAlreadyUsed': 'auth.errors.codeAlreadyUsed',
  'auth.emailAlreadyVerified': 'auth.errors.emailAlreadyVerified',
  'auth.resendTooSoon': 'auth.errors.resendTooSoon',
  'auth.deliveryFailed': 'auth.errors.deliveryFailed',
  'auth.rateLimited': 'auth.errors.rateLimited',
  'auth.invalidResetToken': 'auth.errors.invalidResetToken',
  'auth.unauthorized': 'auth.errors.unauthorized',
  'auth.forbidden': 'auth.errors.forbidden',
  'auth.passwordTooShort': 'auth.errors.passwordTooShort',
  'auth.passwordRequiresNonAlphanumeric': 'auth.errors.passwordRequiresNonAlphanumeric',
  'auth.passwordRequiresLowercase': 'auth.errors.passwordRequiresLowercase',
  'auth.passwordRequiresUppercase': 'auth.errors.passwordRequiresUppercase',
  'auth.passwordRequiresDigit': 'auth.errors.passwordRequiresDigit',
  'auth.passwordRequiresUniqueChars': 'auth.errors.passwordRequiresUniqueChars',
  'error.validation': 'auth.errors.validation',
  'error.unexpected': 'auth.errors.generic',
};

/** Field-targeted codes for registration / reset forms. */
const AUTH_FIELD_BY_ERROR_CODE: Record<string, string> = {
  'auth.emailAlreadyExists': 'email',
  'auth.duplicateEmail': 'email',
  'auth.invalidEmail': 'email',
  'auth.duplicatePhone': 'phoneNumber',
  'auth.passwordTooShort': 'password',
  'auth.passwordRequiresNonAlphanumeric': 'password',
  'auth.passwordRequiresLowercase': 'password',
  'auth.passwordRequiresUppercase': 'password',
  'auth.passwordRequiresDigit': 'password',
  'auth.passwordRequiresUniqueChars': 'password',
  'auth.invalidResetToken': 'token',
};

const PASSWORD_FIELD_ALIASES: Record<string, string> = {
  password: 'password',
  newPassword: 'newPassword',
};

export interface AuthFormServerErrorState {
  /** Codes that should appear in the form summary / toast (form-level). */
  summaryCodes: string[];
  /** Field name → translated messages for that field. */
  fieldMessages: Record<string, string[]>;
  /** All translated messages (deduped) for summary UI. */
  summaryItems: FormErrorSummaryItem[];
}

export function resolveAuthErrorKeys(errorCodes: string[] | undefined): string[] {
  if (!errorCodes?.length) {
    return ['auth.errors.generic'];
  }

  return errorCodes.map((code) => AUTH_ERROR_CODE_TO_KEY[code] ?? 'auth.errors.generic');
}

export function firstAuthErrorKey(errorCodes: string[] | undefined): string {
  return resolveAuthErrorKeys(errorCodes)[0];
}

export function translateAuthErrorCodes(
  transloco: TranslocoService,
  errorCodes: string[] | undefined,
): string {
  const keys = resolveAuthErrorKeys(errorCodes);
  return keys.map((key) => transloco.translate(key)).join(' ');
}

export function translateAuthErrorCodeList(
  transloco: TranslocoService,
  errorCodes: string[] | undefined,
): string[] {
  const seen = new Set<string>();
  const messages: string[] = [];

  for (const key of resolveAuthErrorKeys(errorCodes)) {
    const message = transloco.translate(key);
    if (!seen.has(message)) {
      seen.add(message);
      messages.push(message);
    }
  }

  return messages;
}

/**
 * Splits API error codes into field-level vs form-level messages.
 * @param passwordFieldName Use `newPassword` on reset-password forms.
 */
export function mapAuthServerErrors(
  transloco: TranslocoService,
  errorCodes: string[] | undefined,
  options?: { passwordFieldName?: string; fieldIdPrefix?: string },
): AuthFormServerErrorState {
  const passwordFieldName = options?.passwordFieldName ?? 'password';
  const fieldIdPrefix = options?.fieldIdPrefix ?? '';
  const codes = errorCodes?.length ? [...new Set(errorCodes)] : [];

  if (!codes.length) {
    const generic = transloco.translate('auth.errors.generic');
    return {
      summaryCodes: [],
      fieldMessages: {},
      summaryItems: [{ message: generic }],
    };
  }

  const fieldMessages: Record<string, string[]> = {};
  const summaryCodes: string[] = [];
  const summaryItems: FormErrorSummaryItem[] = [];
  const seenMessages = new Set<string>();

  for (const code of codes) {
    const message = transloco.translate(AUTH_ERROR_CODE_TO_KEY[code] ?? 'auth.errors.generic');
    let field = AUTH_FIELD_BY_ERROR_CODE[code];

    if (field === 'password') {
      field = PASSWORD_FIELD_ALIASES[passwordFieldName] ?? passwordFieldName;
    }

    if (field) {
      fieldMessages[field] ??= [];
      if (!fieldMessages[field].includes(message)) {
        fieldMessages[field].push(message);
      }
    } else {
      summaryCodes.push(code);
    }

    if (!seenMessages.has(message)) {
      seenMessages.add(message);
      summaryItems.push({
        message,
        fieldId: field ? `${fieldIdPrefix}${field}` : undefined,
      });
    }
  }

  return { summaryCodes, fieldMessages, summaryItems };
}

/** Prefer HTTP body `errorCodes`, then successful `ApiResult` failures. */
export function resolveAuthFailureCodes(
  resultOrError: unknown,
  resultErrorCodes?: string[] | null,
): string[] {
  if (resultErrorCodes?.length) {
    return resultErrorCodes;
  }

  return extractApiFailure(resultOrError).errorCodes;
}
