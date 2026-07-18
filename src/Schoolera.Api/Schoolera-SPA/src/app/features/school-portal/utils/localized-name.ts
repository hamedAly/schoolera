import { SchooleraLang } from '../../../core/i18n/schoolera-lang';

export function localizedBilingualName(
  nameAr: string | null | undefined,
  nameEn: string | null | undefined,
  lang: SchooleraLang,
): string {
  if (lang === 'en' && nameEn?.trim()) {
    return nameEn.trim();
  }
  return nameAr?.trim() ?? '';
}

export function localizedOptionalName(
  nameAr: string | null | undefined,
  nameEn: string | null | undefined,
  lang: SchooleraLang,
): string {
  if (lang === 'en' && nameEn?.trim()) {
    return nameEn.trim();
  }
  return nameAr?.trim() ?? '';
}
