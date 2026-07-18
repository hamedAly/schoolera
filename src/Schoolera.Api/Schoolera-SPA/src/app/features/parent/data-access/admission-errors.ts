import { TranslocoService } from '@jsverse/transloco';

import { FormErrorSummaryItem } from '../../../shared/ui/form-error-summary/form-error-summary';

function admissionErrorKey(code: string): string {
  if (code.startsWith('admission.application.')) {
    return `parent.errors.application.${code.slice('admission.application.'.length)}`;
  }

  if (code.startsWith('admission.appointment.')) {
    return `parent.errors.appointment.${code.slice('admission.appointment.'.length)}`;
  }

  if (code.startsWith('courier.')) {
    return `parent.errors.courier.${code.slice('courier.'.length)}`;
  }

  if (code.startsWith('parent.')) {
    return `parent.errors.${code.slice('parent.'.length)}`;
  }

  return 'parent.errors.generic';
}

/** Maps stable API error codes to Transloco keys. Never parse localized message text. */
export function translateAdmissionErrorCodes(
  transloco: TranslocoService,
  errorCodes: string[] | null | undefined,
): string {
  if (!errorCodes?.length) {
    return transloco.translate('parent.errors.generic');
  }

  const messages = errorCodes.map((code) => {
    const key = admissionErrorKey(code);
    const translated = transloco.translate(key);
    return translated === key ? transloco.translate('parent.errors.generic') : translated;
  });

  return [...new Set(messages)].join(' ');
}

export function mapAdmissionErrorSummaryItems(
  transloco: TranslocoService,
  errorCodes: string[] | null | undefined,
): FormErrorSummaryItem[] {
  if (!errorCodes?.length) {
    return [{ message: transloco.translate('parent.errors.generic') }];
  }

  return [...new Set(errorCodes)].map((code) => {
    const key = admissionErrorKey(code);
    const translated = transloco.translate(key);
    return {
      message: translated === key ? transloco.translate('parent.errors.generic') : translated,
    };
  });
}
