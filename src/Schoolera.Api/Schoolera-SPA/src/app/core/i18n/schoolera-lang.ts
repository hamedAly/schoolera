export const SCHOOLERA_SUPPORTED_LANGS = ['ar', 'en'] as const;

export type SchooleraLang = (typeof SCHOOLERA_SUPPORTED_LANGS)[number];

export type SchooleraDirection = 'rtl' | 'ltr';

export type SchooleraLocale = 'ar-EG' | 'en-US';

export const SCHOOLERA_DEFAULT_LANG: SchooleraLang = 'ar';

export const SCHOOLERA_FALLBACK_LANG: SchooleraLang = 'ar';

export const SCHOOLERA_LANG_STORAGE_KEY = 'schoolera.lang';

export const SCHOOLERA_LANG_TO_LOCALE: Record<SchooleraLang, SchooleraLocale> = {
  ar: 'ar-EG',
  en: 'en-US',
};

export function isSchooleraLang(value: string | null | undefined): value is SchooleraLang {
  return value === 'ar' || value === 'en';
}

/** Normalize persisted/browser values; invalid input falls back to Arabic. */
export function resolveSchooleraLang(value: string | null | undefined): SchooleraLang {
  if (!value) {
    return SCHOOLERA_DEFAULT_LANG;
  }

  const normalized = value.trim().toLowerCase();
  if (normalized === 'ar' || normalized.startsWith('ar-')) {
    return 'ar';
  }

  if (normalized === 'en' || normalized.startsWith('en-')) {
    return 'en';
  }

  return SCHOOLERA_FALLBACK_LANG;
}

export function schooleraDocumentDirection(lang: string): SchooleraDirection {
  return resolveSchooleraLang(lang) === 'ar' ? 'rtl' : 'ltr';
}

export function schooleraLocale(lang: string): SchooleraLocale {
  return SCHOOLERA_LANG_TO_LOCALE[resolveSchooleraLang(lang)];
}

/** Accept-Language value sent to the API (short tags). */
export function schooleraAcceptLanguage(lang: string): SchooleraLang {
  return resolveSchooleraLang(lang);
}
