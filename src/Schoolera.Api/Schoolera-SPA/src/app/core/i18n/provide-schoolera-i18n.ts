import { isDevMode } from '@angular/core';
import { provideTransloco, provideTranslocoScope } from '@jsverse/transloco';
import { provideTranslocoLocale } from '@jsverse/transloco-locale';
import { provideTranslocoPersistLang } from '@jsverse/transloco-persist-lang';

import {
  SCHOOLERA_DEFAULT_LANG,
  SCHOOLERA_LANG_STORAGE_KEY,
  SCHOOLERA_SUPPORTED_LANGS,
  resolveSchooleraLang,
} from './schoolera-lang';
import { TranslocoHttpLoader } from './transloco-loader';

export function provideSchooleraI18n() {
  return [
    provideTransloco({
      config: {
        availableLangs: [...SCHOOLERA_SUPPORTED_LANGS],
        defaultLang: SCHOOLERA_DEFAULT_LANG,
        fallbackLang: SCHOOLERA_DEFAULT_LANG,
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
        missingHandler: {
          logMissingKey: isDevMode(),
          useFallbackTranslation: true,
        },
      },
      loader: TranslocoHttpLoader,
    }),
    provideTranslocoScope('auth'),
    provideTranslocoScope('schools'),
    provideTranslocoScope('admin'),
    provideTranslocoScope('onboarding'),
    provideTranslocoScope('portal'),
    provideTranslocoScope('parent'),
    provideTranslocoScope('support'),
    provideTranslocoLocale({
      langToLocaleMapping: {
        ar: 'ar-EG',
        en: 'en-US',
      },
      defaultLocale: 'ar-EG',
    }),
    provideTranslocoPersistLang({
      storageKey: SCHOOLERA_LANG_STORAGE_KEY,
      storage: {
        useValue: localStorage,
      },
      getLangFn: ({ cachedLang, defaultLang }) => resolveSchooleraLang(cachedLang ?? defaultLang),
    }),
  ];
}
