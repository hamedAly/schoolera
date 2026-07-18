import { TranslocoService } from '@jsverse/transloco';

import { FormErrorSummaryItem } from '../../../shared/ui/form-error-summary/form-error-summary';

function parentErrorKey(code: string): string {
  if (code.startsWith('parent.')) {
    return `parent.errors.${code.slice('parent.'.length)}`;
  }
  if (code.startsWith('favorites.')) {
    return `parent.errors.favorites.${code.slice('favorites.'.length)}`;
  }
  if (code.startsWith('supportTicket.')) {
    return `parent.errors.supportTicket.${code.slice('supportTicket.'.length)}`;
  }
  return 'parent.errors.generic';
}

/** Maps stable API error codes to Transloco keys. Never parse localized message text. */
export function translateParentErrorCodes(
  transloco: TranslocoService,
  errorCodes: string[] | null | undefined,
): string {
  if (!errorCodes?.length) {
    return transloco.translate('parent.errors.generic');
  }

  const messages = errorCodes.map((code) => {
    const key = parentErrorKey(code);
    const translated = transloco.translate(key);
    return translated === key ? transloco.translate('parent.errors.generic') : translated;
  });

  return [...new Set(messages)].join(' ');
}

export function mapParentErrorSummaryItems(
  transloco: TranslocoService,
  errorCodes: string[] | null | undefined,
): FormErrorSummaryItem[] {
  if (!errorCodes?.length) {
    return [{ message: transloco.translate('parent.errors.generic') }];
  }

  return [...new Set(errorCodes)].map((code) => ({
    message: (() => {
      const key = parentErrorKey(code);
      const translated = transloco.translate(key);
      return translated === key ? transloco.translate('parent.errors.generic') : translated;
    })(),
  }));
}
