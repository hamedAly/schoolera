import { translateAdmissionErrorCodes } from './admission-errors';

describe('admission-errors', () => {
  const transloco = {
    translate: (key: string) => {
      const map: Record<string, string> = {
        'parent.errors.generic': 'generic',
        'parent.errors.application.admissionClosed': 'closed',
        'parent.errors.application.studentNotOwned': 'not-owned',
      };
      return map[key] ?? key;
    },
  };

  it('maps admission.application codes to parent.errors.application keys', () => {
    const message = translateAdmissionErrorCodes(transloco as never, [
      'admission.application.admissionClosed',
      'admission.application.studentNotOwned',
    ]);
    expect(message).toContain('closed');
    expect(message).toContain('not-owned');
  });

  it('falls back to generic for unknown codes', () => {
    expect(translateAdmissionErrorCodes(transloco as never, ['something.else'])).toBe('generic');
  });
});
