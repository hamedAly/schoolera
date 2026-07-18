import { TranslocoService } from '@jsverse/transloco';

const ONBOARDING_ERROR_PREFIX = 'onboarding.errors.';

/** Maps stable API error codes to Transloco keys. Never parse localized message text. */
export function translateOnboardingErrorCodes(
  transloco: TranslocoService,
  errorCodes: string[] | null | undefined,
): string {
  if (!errorCodes?.length) {
    return transloco.translate('onboarding.errors.generic');
  }

  const messages = errorCodes.map((code) => {
    const key = `${ONBOARDING_ERROR_PREFIX}${code.replace(/^onboarding\./, '')}`;
    const translated = transloco.translate(key);
    return translated === key ? transloco.translate('onboarding.errors.generic') : translated;
  });

  return [...new Set(messages)].join(' ');
}
