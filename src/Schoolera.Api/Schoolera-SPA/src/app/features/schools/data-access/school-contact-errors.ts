import { TranslocoService } from '@jsverse/transloco';

import { FormErrorSummaryItem } from '../../../shared/ui/form-error-summary/form-error-summary';

const SCHOOL_CONTACT_ERROR_CODE_TO_KEY: Record<string, string> = {
  'school.not_found': 'schools.profile.contact.errors.notFound',
  'school.contact.consent_required': 'schools.profile.contact.errors.consentRequired',
  'school.contact.invalid_source': 'schools.profile.contact.errors.invalidSource',
  'school.contact.rejected': 'schools.profile.contact.errors.rejected',
  'error.validation': 'schools.profile.contact.errors.validation',
  'error.unexpected': 'schools.profile.contact.errors.generic',
};

const SCHOOL_CONTACT_FIELD_BY_ERROR_CODE: Record<string, string> = {
  'school.contact.consent_required': 'contact-consent',
};

export interface SchoolContactFormServerErrorState {
  fieldMessages: Record<string, string[]>;
  summaryItems: FormErrorSummaryItem[];
}

export function mapSchoolContactServerErrors(
  transloco: TranslocoService,
  errorCodes: string[] | undefined,
): SchoolContactFormServerErrorState {
  const codes = errorCodes?.length ? [...new Set(errorCodes)] : [];

  if (!codes.length) {
    const generic = transloco.translate('schools.profile.contact.errors.generic');
    return {
      fieldMessages: {},
      summaryItems: [{ message: generic }],
    };
  }

  const fieldMessages: Record<string, string[]> = {};
  const summaryItems: FormErrorSummaryItem[] = [];
  const seenMessages = new Set<string>();

  for (const code of codes) {
    const message = transloco.translate(
      SCHOOL_CONTACT_ERROR_CODE_TO_KEY[code] ?? 'schools.profile.contact.errors.generic',
    );
    const field = SCHOOL_CONTACT_FIELD_BY_ERROR_CODE[code];

    if (field) {
      fieldMessages[field] ??= [];
      if (!fieldMessages[field].includes(message)) {
        fieldMessages[field].push(message);
      }
    }

    if (!seenMessages.has(message)) {
      seenMessages.add(message);
      summaryItems.push({
        message,
        fieldId: field,
      });
    }
  }

  return { fieldMessages, summaryItems };
}
