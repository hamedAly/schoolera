import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideTranslocoLocale } from '@jsverse/transloco-locale';
import { TranslocoTestingModule } from '@jsverse/transloco';

import { acceptLanguageInterceptor } from '../http/accept-language.interceptor';
import { DocumentLanguageService } from './document-language.service';
import { LocaleFormatService } from './locale-format.service';
import { SCHOOLERA_LANG_STORAGE_KEY } from './schoolera-lang';

describe('DocumentLanguageService', () => {
  beforeEach(() => {
    localStorage.removeItem(SCHOOLERA_LANG_STORAGE_KEY);
  });

  async function setup(initialStorage?: string) {
    if (initialStorage !== undefined) {
      localStorage.setItem(SCHOOLERA_LANG_STORAGE_KEY, initialStorage);
    }

    await TestBed.configureTestingModule({
      imports: [
        TranslocoTestingModule.forRoot({
          langs: { ar: {}, en: {} },
          translocoConfig: {
            availableLangs: ['ar', 'en'],
            defaultLang: 'ar',
            reRenderOnLangChange: true,
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideTranslocoLocale({
          langToLocaleMapping: { ar: 'ar-EG', en: 'en-US' },
          defaultLocale: 'ar-EG',
        }),
        DocumentLanguageService,
        LocaleFormatService,
        provideHttpClient(withInterceptors([acceptLanguageInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    return {
      language: TestBed.inject(DocumentLanguageService),
      http: TestBed.inject(HttpTestingController),
      format: TestBed.inject(LocaleFormatService),
      client: TestBed.inject(HttpClient),
    };
  }

  it('defaults to Arabic RTL when nothing is stored', async () => {
    const { language } = await setup();
    expect(language.activeLang()).toBe('ar');
    expect(language.direction()).toBe('rtl');
    expect(language.locale()).toBe('ar-EG');
    expect(document.documentElement.lang).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('falls back to Arabic for invalid stored language', async () => {
    const { language } = await setup('de-DE');
    expect(language.readPersistedOrDefault()).toBe('ar');
  });

  it('switches to English LTR without changing the route path', async () => {
    const { language } = await setup();
    const pathBefore = window.location.pathname;
    language.setLanguage('en');
    expect(language.activeLang()).toBe('en');
    expect(document.documentElement.lang).toBe('en');
    expect(document.documentElement.dir).toBe('ltr');
    expect(window.location.pathname).toBe(pathBefore);
  });

  it('sends Accept-Language matching the active language for HttpClient requests', async () => {
    const { language, http, client } = await setup();
    language.setLanguage('en');

    client.get('/api/schools').subscribe();
    const request = http.expectOne('/api/schools');
    expect(request.request.headers.get('Accept-Language')).toBe('en');
    expect(request.request.url).toBe('/api/schools');
    request.flush({ succeeded: true, data: [], errors: [], errorCodes: [] });
  });

  it('formats numbers using the active locale', async () => {
    const { language, format } = await setup();
    language.setLanguage('en');
    expect(format.formatNumber(1234.5)).toContain('1');
    language.setLanguage('ar');
    expect(format.formatNumber(1234.5).length).toBeGreaterThan(0);
  });
});
