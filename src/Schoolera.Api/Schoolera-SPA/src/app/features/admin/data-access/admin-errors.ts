import { TranslocoService } from '@jsverse/transloco';

function adminErrorKey(code: string): string {
  if (code.startsWith('admin.')) {
    return `admin.errors.${code.slice('admin.'.length)}`;
  }
  if (code.startsWith('onboarding.')) {
    return `admin.errors.onboarding.${code.slice('onboarding.'.length)}`;
  }
  if (code.startsWith('cms.')) {
    return `admin.errors.cms.${code.slice('cms.'.length)}`;
  }
  if (code.startsWith('contact.')) {
    return `admin.errors.contact.${code.slice('contact.'.length)}`;
  }
  if (code.startsWith('integrations.')) {
    return `admin.errors.integrations.${code.slice('integrations.'.length)}`;
  }
  if (code.startsWith('courier.')) {
    return `admin.errors.courier.${code.slice('courier.'.length)}`;
  }
  if (code.startsWith('supportTicket.')) {
    return `admin.errors.supportTicket.${code.slice('supportTicket.'.length)}`;
  }
  return 'admin.errors.generic';
}

/** Maps stable API error codes to Transloco keys. Never parse localized message text. */
export function translateAdminErrorCodes(
  transloco: TranslocoService,
  errorCodes: string[] | null | undefined,
): string {
  if (!errorCodes?.length) {
    return transloco.translate('admin.errors.generic');
  }

  const messages = errorCodes.map((code) => {
    const key = adminErrorKey(code);
    const translated = transloco.translate(key);
    return translated === key ? transloco.translate('admin.errors.generic') : translated;
  });

  return [...new Set(messages)].join(' ');
}
